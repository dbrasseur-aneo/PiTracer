# -*- coding: utf-8 -*-
import argparse
import grpc
import numpy as np
import sys

import math
import random
import cv2
from threading import Thread
from multiprocessing import JoinableQueue, Process
import time

from armonik.client import ArmoniKSubmitter
from armonik.common import TaskDefinition, TaskOptions, TaskStatus
from armonik.protogen.common.submitter_common_pb2 import GetResultStatusRequest
from armonik.protogen.common.result_status_pb2 import RESULT_STATUS_ABORTED, RESULT_STATUS_COMPLETED, RESULT_STATUS_CREATED, RESULT_STATUS_NOTFOUND, RESULT_STATUS_UNSPECIFIED
from datetime import timedelta

class Reflection:
    DIFF = 0
    SPEC = 1
    REFR = 2


class Sphere:
    def __init__(self, radius=0.0, position=[0.0, 0.0, 0.0], emission=[0.0, 0.0, 0.0], color=[0.0, 0.0, 0.0], reflection=Reflection.DIFF, max_reflectivity=-1.0):
        self.radius = radius
        self.position = position
        self.emission = emission
        self.color = color
        self.reflection = reflection
        self.max_reflectivity = max_reflectivity

    def to_bytes(self):
        return np.array(
            [self.radius, self.position[0], self.position[1], self.position[2],
             self.emission[0], self.emission[1], self.emission[2],
             self.color[0], self.color[1], self.color[2]], dtype=np.float32).tobytes()+ self.reflection.to_bytes(4, 'little') + np.array([self.max_reflectivity], dtype=np.float32).tobytes()


class TracerPayload:

    def __init__(self, args, coord_x=0, coord_y=0):
        self.img_height = args.height
        self.img_width = args.width
        self.samples = args.samples
        self.kill_depth = args.killdepth
        self.split_depth = args.splitdepth
        self.task_width = args.taskwidth
        self.task_height = args.taskheight
        self.coord_x = coord_x
        self.coord_y = coord_y
        self.spheres = []
        self.camera = Camera()


class TracerResult:
    def __init__(self, result):
        self.coord_x = int.from_bytes(result[0:4], 'little')
        self.coord_y = int.from_bytes(result[4:8], 'little')
        self.task_width = int.from_bytes(result[8:12], 'little')
        self.task_height = int.from_bytes(result[12:16], 'little')
        self.pixels = list(result[16:])

    def pixels_to_numpy_array(self) -> np.ndarray:
        return np.flip(np.array(self.pixels, np.uint8).reshape((self.task_height, self.task_width, 3)), axis=0)


class Camera:
    def __init__(self, length=140, cst=0.4635, position=None, direction=None):
        if position is None:
            position = [50, 52, 295.6]
        if direction is None:
            direction = [0, -0.072612, -1]
        self.length = length
        self.cst = cst
        self.position = position
        self.direction = direction

    def to_bytes(self):
        return np.array([self.length, self.cst, self.position[0], self.position[1], self.position[2], self.direction[0], self.direction[1], self.direction[2]], dtype=np.float32).tobytes()


def parse_args():
    parser = argparse.ArgumentParser(description='Client for PiTracer')
    parser.add_argument('--server_url', help='server url')
    parser.add_argument('--height', help='height of image', default=160, type=int)
    parser.add_argument('--width', help='width of image', default=256, type=int)
    parser.add_argument('--samples', help='number of samples per task', default=300, type=int)
    parser.add_argument('--totalsamples', help='minimum number of samples per pixel', default=300, type=int)
    parser.add_argument('--killdepth', help='ray kill depth', default=7, type=int)
    parser.add_argument('--splitdepth', help='ray split depth', default=2, type=int)
    parser.add_argument('--taskheight', help='height of a task in pixels', default=32, type=int)
    parser.add_argument('--taskwidth', help="width of a task in pixels", default=32, type=int)
    return parser.parse_args()


spheres = \
    [Sphere(1e5, [1e5 - 5, 40.8, 81.6], [0.0, 0.0, 0.0], [.75, .25, .25], Reflection.DIFF, -1),
        Sphere(1e5, [-1e5 + 104, 40.8, 81.6], [0.0, 0.0, 0.0], [.25, .25, .75], Reflection.DIFF, -1),
        Sphere(1e5, [50, 40.8, 1e5], [0.0, 0.0, 0.0], [.75, .75, .75], Reflection.DIFF, -1),
        Sphere(1e5, [50, 40.8, -1e5 + 170], [0.0, 0.0, 0.0], [0.0, 0.0, 0.0], Reflection.DIFF, -1),
        Sphere(1e5, [50, 1e5, 81.6], [0.0, 0.0, 0.0], [0.75, .75, .75], Reflection.DIFF, -1),
        Sphere(1e5, [50, -1e5 + 81.6, 81.6], [0.0, 0.0, 0.0], [0.75, .75, .75], Reflection.DIFF, -1),
        Sphere(16.5, [40, 16.5, 47], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.SPEC, -1),
        Sphere(10, [90, 25, 125], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.SPEC, -1),
        Sphere(16.5, [73, 46.5, 94], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.REFR, -1),
        Sphere(10, [15, 45, 112], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.DIFF, -1),
        Sphere(15, [16, 16, 130], [0.0, 0.0, 0.0], [0, .999, 0], Reflection.REFR, -1),
        Sphere(7.5, [40, 8, 120], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
        Sphere(8.5, [67, 9, 122], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
        Sphere(10, [80, 12, 92], [0.1, 0.9, 0.1], [0, .999, 0], Reflection.DIFF, -1),
        Sphere(600, [50, 681.33, 81.6], [0.8, 0.8, 0.8], [0.0, 0.0, 0.0], Reflection.DIFF, -1),
        Sphere(9, [95, 65, 81.6], [0.0, 0.6, 0.9], [0, .682, .999], Reflection.DIFF, -1),
        Sphere(8, [15, 70, 75], [0.9, 0.1, 0.1], [.999, 0.1, 0.1], Reflection.DIFF, -1)]

camera = Camera()

def get_payload(args, coord_x, coord_y):
    payload = TracerPayload(args, coord_x, coord_y)
    payload.task_width = max(0, min(payload.img_width - payload.coord_y, payload.task_width))
    payload.task_height = max(0, min(payload.img_height - payload.coord_x, payload.task_height))
    payload.spheres = spheres
    payload.camera = camera
    return payload


def to_byte(payload):
    #return base64.b64encode(bytes(json.dumps(payload, default=vars), 'UTF-8'))
    pb = []
    pb.extend(payload.img_width.to_bytes(4, 'little'))
    pb.extend(payload.img_height.to_bytes(4, 'little'))
    pb.extend(payload.coord_x.to_bytes(4, 'little'))
    pb.extend(payload.coord_y.to_bytes(4, 'little'))
    pb.extend(payload.kill_depth.to_bytes(4, 'little'))
    pb.extend(payload.split_depth.to_bytes(4, 'little'))
    pb.extend(payload.task_width.to_bytes(4, 'little'))
    pb.extend(payload.task_height.to_bytes(4, 'little'))
    pb.extend(payload.samples.to_bytes(4, 'little'))
    pb.extend(payload.camera.to_bytes())
    for s in payload.spheres:
        pb.extend(s.to_bytes())
    return bytes(pb)

def get_payloads(args) -> list[bytes]:
    img_height = args.height
    img_width = args.width
    task_width = args.taskwidth
    task_height = args.taskheight
    n_rows = int(math.ceil(img_height/task_height))
    n_cols = int(math.ceil(img_width/task_width))
    n_times = int(math.ceil(args.totalsamples/args.samples))
    coord_list = [(i*task_height, j*task_width) for _ in range(n_times) for i in range(n_rows) for j in range(n_cols)]
    random.shuffle(coord_list)
    return [to_byte(get_payload(args, i, j)) for i, j in coord_list]


class ResultHandler:
    def __init__(self, session, stub : ArmoniKSubmitter, total_height, total_width, overlay:bool):
        self.img = np.zeros((total_height, total_width, 3), np.uint8)
        self.session_id = session
        self.stub = stub
        self.done = False
        self.imwidth = total_width
        self.imheight = total_height
        self.need_refresh = False
        self.to_process_queue = JoinableQueue()
        self.task_future_mapping = {}
        self.task_done={}
        self.cancelled = False
        self.in_progress = []
        self.overlay = overlay

    def refresh_display(self):
        cv2.namedWindow("Display")
        cv2.setWindowProperty("Display", cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
        cv2.imshow("Display", self.img)
        cv2.waitKey(1)
        while (not self.cancelled) and ((not self.done) or self.need_refresh or not self.to_process_queue.empty()):
            if not self.to_process_queue.empty():
                self.process(self.to_process_queue.get())
                self.to_process_queue.task_done()
            if self.need_refresh:
                cv2.imshow("Display", self.img)
            self.need_refresh = False
            cv2.waitKey(1)
        if not self.cancelled:
            cv2.waitKey(0)

        
    def add_to_queue(self, result_id):
        self.to_process_queue.put(result_id)

    def reset(self):
        self.img.fill(0)
        self.task_done.clear()
        self.need_refresh=True

    def copy_to_img(self, result):
        """

        :param result:
        :type result TracerResult
        :return:
        """
        #print(f'CoordX {result.coord_x} CoordY {result.coord_y} TaskHeight {result.task_height} TaskWidth {result.task_width}')
        coords = (result.coord_x, result.coord_y)
        if coords not in self.task_done:
            self.task_done[coords] = 0
        n_times = self.task_done[coords]
        if self.overlay:
            chunk = self.img[
                self.imheight-result.coord_x - result.task_height:self.imheight-result.coord_x,
                result.coord_y:result.coord_y + result.task_width,
                :]
            self.img[
                self.imheight - result.coord_x - result.task_height:self.imheight - result.coord_x,
                result.coord_y:result.coord_y + result.task_width,
                :] = (np.power((n_times * np.power(chunk.astype(float)/255, 2.2) + np.power(result.pixels_to_numpy_array().astype(float)/255, 2.2))/(n_times+1), 1/2.2)*255+0.5).astype(np.uint8)
        else:
            self.img[
                self.imheight - result.coord_x - result.task_height:self.imheight - result.coord_x,
                result.coord_y:result.coord_y + result.task_width,
                :] = result.pixels_to_numpy_array()
        self.task_done[coords] = n_times+1

    def process(self, result_id):
        result = self.stub.get_result(self.session_id, result_id)
        result = TracerResult(result)
        self.copy_to_img(result)
        self.need_refresh = True


def main(args):
    print("Hello PiTracer Demo !")
    if args.server_url is None:
        print("server url is mandatory")
        return
    with grpc.insecure_channel(args.server_url) as channel:
        print("GRPC channel started")
        stub = ArmoniKSubmitter(channel)
        session_id = stub.create_session(TaskOptions(max_duration=timedelta(seconds=300), priority=1, max_retries=5))
        print("Session created")
        result_handler = ResultHandler(session_id, stub, args.height, args.width, args.totalsamples > args.samples)
        thread = Thread(target=result_handler.refresh_display)
        payloads = get_payloads(args)
        print("Payloads created")
        packet_index = 0
        packet_size = 512
        next_batch_thres=384
        task_defs = [TaskDefinition(p, [stub.request_output_id(session_id)]) for p in payloads[packet_index:packet_index+packet_size]]
        task_infos, errs = stub.submit(session_id, task_defs)
        current_packet = dict([(t.id, t.expected_output_ids[0]) for t in task_infos if t.expected_output_ids is not None and t.id is not None])
        results_status : dict[str, str] = {}
        packet_index += packet_size
        #task_ids = session_client.submit_tasks(payloads)
        thread.start()
        count = {
            "Completed": 0,
            "Pending": 0,
            "Processing": 0,
            "AvailableResult":0,
            "ProcessedResult":0
        }
        
        try:
            while len(current_packet) + len(results_status) > 0:
                hasDone = False
                count["Processing"]=0
                count["Pending"]=0
                if len(current_packet) > 0:
                    statuses = stub.get_task_status(list(current_packet.keys()))
                    for task_id, status in statuses.items():
                        if status == TaskStatus.COMPLETED:
                            count["Completed"] += 1
                            results_status[current_packet[task_id]] = task_id
                            hasDone = True
                            if(current_packet.pop(task_id, "KeyNotFound") == "KeyNotFound"):
                                print("Removing unknown task from packet")
                        elif status == TaskStatus.PROCESSING or status == TaskStatus.DISPATCHED:
                            count["Processing"] += 1
                        elif status == TaskStatus.CREATING or status == TaskStatus.SUBMITTED:
                            count["Pending"] += 1
                        elif status == TaskStatus.ERROR:
                            print(f'Task in error : {task_id}')
                            if(current_packet.pop(task_id, "KeyNotFound") == "KeyNotFound"):
                                print("Removing unknown task from packet")
                    if len(current_packet) < next_batch_thres and packet_index < len(payloads):
                        print("Sending new batch")
                        task_defs = [TaskDefinition(p, [stub.request_output_id(session_id)]) for p in payloads[packet_index:packet_index+packet_size]]
                        task_infos, errs = stub.submit(session_id, task_defs)
                        current_packet.update(dict([(t.id, t.expected_output_ids[0]) for t in task_infos if t.expected_output_ids is not None and t.id is not None]))
                        packet_index += packet_size
                if len(results_status) > 0:
                    status_reply = stub._client.GetResultStatus(GetResultStatusRequest(result_ids=list(results_status.keys()), session_id=session_id))
                    for s in status_reply.id_statuses:
                        if s.status == RESULT_STATUS_COMPLETED:
                            result_handler.add_to_queue(s.result_id)
                            count["AvailableResult"] += 1
                            results_status.pop(s.result_id, None)
                        elif s.status == RESULT_STATUS_ABORTED:
                            print(f"Result in error : {s.result_id}")
                            results_status.pop(s.result_id, None)
                print(f'Task statuses : \n Pending : {count["Pending"]}\n Processing : {count["Processing"]}\n Completed : {count["Completed"]}\n Available results : {count["AvailableResult"]}\n Processed results : {count["AvailableResult"]-result_handler.to_process_queue.qsize()}')
                time.sleep(1)
            result_handler.to_process_queue.join()
            print("Demo is done !")
        except BaseException as e:
            print(f'Cancelled : {str(e)}')
            result_handler.cancelled = True
            result_handler.done = True
            stub.cancel_session(session_id)
            print("Tasks successfully canceled")
        finally:
            try:
                result_handler.done = True
                thread.join()
            except KeyboardInterrupt:
                pass
    cv2.destroyAllWindows()


if __name__ == '__main__':
    main(parse_args())
