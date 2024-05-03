# -*- coding: utf-8 -*-
import argparse
import grpc
import numpy as np

import math
import random
import cv2
from threading import Thread
from multiprocessing import JoinableQueue, Process
import time

from armonik.client import ArmoniKSubmitter
from armonik.common import TaskDefinition, TaskOptions, TaskStatus
from armonik.protogen.common.submitter_common_pb2 import GetResultStatusRequest
from armonik.protogen.common.result_status_pb2 import (
    RESULT_STATUS_ABORTED,
    RESULT_STATUS_COMPLETED,
    RESULT_STATUS_CREATED,
    RESULT_STATUS_NOTFOUND,
    RESULT_STATUS_UNSPECIFIED,
)
from datetime import timedelta
from typing import Generator, Union

from tracer.objects import Sphere, Reflection, Camera, TracerPayload, TracerResult


def parse_args():
    parser = argparse.ArgumentParser(description="Client for PiTracer")
    parser.add_argument("--server_url", help="server url")
    parser.add_argument("--height", help="height of image", default=1080, type=int)
    parser.add_argument("--width", help="width of image", default=1920, type=int)
    parser.add_argument(
        "--samples", help="number of samples per task", default=250, type=int
    )
    parser.add_argument(
        "--totalsamples",
        help="minimum number of samples per pixel",
        default=250,
        type=int,
    )
    parser.add_argument("--killdepth", help="ray kill depth", default=7, type=int)
    parser.add_argument("--splitdepth", help="ray split depth", default=1, type=int)
    parser.add_argument(
        "--taskheight", help="height of a task in pixels", default=32, type=int
    )
    parser.add_argument(
        "--taskwidth", help="width of a task in pixels", default=32, type=int
    )
    return parser.parse_args()


spheres = [
    Sphere(
        1e5,
        [1e5 - 5, 40.8, 81.6],
        [0.0, 0.0, 0.0],
        [0.75, 0.25, 0.25],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5,
        [-1e5 + 104, 40.8, 81.6],
        [0.0, 0.0, 0.0],
        [0.25, 0.25, 0.75],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5, [50, 40.8, 1e5], [0.0, 0.0, 0.0], [0.75, 0.75, 0.75], Reflection.DIFF, -1
    ),
    Sphere(
        1e5,
        [50, 40.8, -1e5 + 170],
        [0.0, 0.0, 0.0],
        [0.0, 0.0, 0.0],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5, [50, 1e5, 81.6], [0.0, 0.0, 0.0], [0.75, 0.75, 0.75], Reflection.DIFF, -1
    ),
    Sphere(
        1e5,
        [50, -1e5 + 81.6, 81.6],
        [0.0, 0.0, 0.0],
        [0.75, 0.75, 0.75],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        16.5,
        [40, 16.5, 47],
        [0.0, 0.0, 0.0],
        [0.999, 0.999, 0.999],
        Reflection.SPEC,
        -1,
    ),
    Sphere(
        10, [90, 25, 125], [0.0, 0.0, 0.0], [0.999, 0.999, 0.999], Reflection.SPEC, -1
    ),
    Sphere(
        16.5,
        [73, 46.5, 94],
        [0.0, 0.0, 0.0],
        [0.999, 0.999, 0.999],
        Reflection.REFR,
        -1,
    ),
    Sphere(
        10, [15, 45, 112], [0.0, 0.0, 0.0], [0.999, 0.999, 0.999], Reflection.DIFF, -1
    ),
    Sphere(15, [16, 16, 130], [0.0, 0.0, 0.0], [0, 0.999, 0], Reflection.REFR, -1),
    Sphere(7.5, [40, 8, 120], [0.0, 0.0, 0.0], [0.999, 0.999, 0], Reflection.REFR, -1),
    Sphere(8.5, [67, 9, 122], [0.1, 0.1, 0.0], [0.999, 0.999, 0], Reflection.REFR, -1),
    Sphere(10, [80, 12, 92], [0.1, 1, 0.1], [0.1, 0.6, 0.1], Reflection.DIFF, -1),
    Sphere(
        600, [50, 681.33, 81.6], [1.5, 1.5, 1.5], [0.0, 0.0, 0.0], Reflection.DIFF, -1
    ),
    Sphere(9, [95, 65, 81.6], [0.0, 0.4, 0.8], [0.1, 0.3, 0.6], Reflection.DIFF, -1),
    Sphere(8, [15, 70, 75], [1, 0.1, 0.1], [0.6, 0.1, 0.1], Reflection.DIFF, -1),
]

camera = Camera()


def get_payload(args, coord_x, coord_y):
    payload = TracerPayload(args, coord_x, coord_y)
    payload.task_width = max(
        0, min(payload.img_width - payload.coord_y, payload.task_width)
    )
    payload.task_height = max(
        0, min(payload.img_height - payload.coord_x, payload.task_height)
    )
    payload.spheres = spheres
    payload.camera = camera
    return payload


def get_num_payloads(args) -> int:
    img_height = args.height
    img_width = args.width
    task_width = args.taskwidth
    task_height = args.taskheight
    n_rows = int(math.ceil(img_height / task_height))
    n_cols = int(math.ceil(img_width / task_width))
    n_times = int(math.ceil(args.totalsamples / args.samples))
    return n_rows * n_cols * n_times


def get_payloads(args) -> Generator[bytes, None, None]:
    img_height = args.height
    img_width = args.width
    task_width = args.taskwidth
    task_height = args.taskheight
    n_rows = int(math.ceil(img_height / task_height))
    n_cols = int(math.ceil(img_width / task_width))
    n_times = int(math.ceil(args.totalsamples / args.samples))
    coord_list = [
        (i * task_height, j * task_width)
        for _ in range(n_times)
        for i in range(n_rows)
        for j in range(n_cols)
    ]
    random.shuffle(coord_list)
    for i, j in coord_list:
        yield get_payload(args, i, j).to_byte()


class ResultHandler:
    def __init__(
        self,
        session: Union[str, None],
        stub: Union[ArmoniKSubmitter, None],
        total_height,
        total_width,
        overlay: bool,
    ):
        self.img = np.zeros((total_height, total_width, 3), np.uint8)
        self.session_id = session
        self.stub = stub
        self.done = False
        self.imwidth = total_width
        self.imheight = total_height
        self.need_refresh = False
        self.to_process_queue = JoinableQueue()
        self.task_future_mapping = {}
        self.task_done = {}
        self.cancelled = False
        self.in_progress = []
        self.overlay = overlay

    def refresh_display(self):
        cv2.namedWindow("Display")
        cv2.setWindowProperty("Display", cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
        cv2.imshow("Display", self.img)
        cv2.waitKey(1)
        while (not self.cancelled) and (
            (not self.done) or self.need_refresh or not self.to_process_queue.empty()
        ):
            if not self.to_process_queue.empty():
                self.process(self.to_process_queue.get())
                self.to_process_queue.task_done()
            if self.need_refresh:
                cv2.imshow("Display", self.img)
            self.need_refresh = False
            cv2.waitKey(1)
        key = -1
        while key == -1 and not self.cancelled:
            cv2.waitKey(16)
        cv2.destroyAllWindows()

    def add_to_queue(self, result_id):
        self.to_process_queue.put(result_id)

    def reset(self):
        self.img.fill(0)
        self.task_done.clear()
        self.need_refresh = True

    def copy_to_img(self, result):
        """

        :param result:
        :type result TracerResult
        :return:
        """
        coords = (result.coord_x, result.coord_y)
        if coords not in self.task_done:
            self.task_done[coords] = 0
        n_times = self.task_done[coords]
        if self.overlay:
            chunk = self.img[
                self.imheight
                - result.coord_x
                - result.task_height : self.imheight
                - result.coord_x,
                result.coord_y : result.coord_y + result.task_width,
                :,
            ]
            self.img[
                self.imheight
                - result.coord_x
                - result.task_height : self.imheight
                - result.coord_x,
                result.coord_y : result.coord_y + result.task_width,
                :,
            ] = (
                np.power(
                    (
                        n_times * np.power(chunk.astype(float) / 255, 2.2)
                        + np.power(
                            result.pixels_to_numpy_array().astype(float) / 255, 2.2
                        )
                    )
                    / (n_times + 1),
                    1 / 2.2,
                )
                * 255
                + 0.5
            ).astype(
                np.uint8
            )
        else:
            self.img[
                self.imheight
                - result.coord_x
                - result.task_height : self.imheight
                - result.coord_x,
                result.coord_y : result.coord_y + result.task_width,
                :,
            ] = result.pixels_to_numpy_array()
        self.task_done[coords] = n_times + 1

    def process(self, result_id):
        if self.stub is not None and self.session_id is not None:
            result = self.stub.get_result(self.session_id, result_id)
            result = TracerResult(result)
            self.copy_to_img(result)
            self.need_refresh = True


class TaskHandler:
    def __init__(self, session_id: Union[str, None], stub, packet_size, args):
        self.session = session_id
        self._client: ArmoniKSubmitter = stub
        self.args = args
        self.packet_size = packet_size
        self.n_prepared = 0
        self.payloads: list[Union[bytes, None]] = [None] * get_num_payloads(args)
        self.task_result_mapping: dict[str, str] = {}
        self.max_pending = 384
        self.cancelled = False
        self.current_index = 0
        self.done = False
        self.errors: list[str] = []

    def prepare_payloads(self):
        for i, p in enumerate(get_payloads(self.args)):
            if self.cancelled:
                break
            self.payloads[i] = p
            self.n_prepared += 1
            if self.n_prepared - self.current_index > self.packet_size:
                time.sleep(0.000001)

    def auto_send(self):
        self.done = False
        self.current_index = 0
        self.errors.clear()
        self.task_result_mapping.clear()
        while self.current_index < len(self.payloads) and not self.cancelled:
            if (
                self.session is not None
                and len(self.task_result_mapping) < self.max_pending
            ):
                task_defs = [
                    TaskDefinition(p, [self._client.request_output_id(self.session)])
                    for p in self.payloads[
                        self.current_index : self.current_index + self.packet_size
                    ]
                    if p is not None
                ]
                if len(task_defs) > 0:
                    task_infos, errs = self._client.submit(self.session, task_defs)
                    self.errors.extend(errs)
                    self.task_result_mapping.update(
                        {
                            t.id: t.expected_output_ids[0]
                            for t in task_infos
                            if t.expected_output_ids is not None and t.id is not None
                        }
                    )
                    self.current_index += len(task_defs)
                else:
                    time.sleep(0.1)
            else:
                time.sleep(0.1)
        self.done = True


def main(args):
    print("Hello PiTracer Demo !")
    if args.server_url is None:
        print("server url is mandatory")
        return
    with grpc.insecure_channel(args.server_url) as channel:
        print("GRPC channel started")
        task_handler = TaskHandler(None, None, 64, args)
        result_handler = ResultHandler(
            None, None, args.height, args.width, args.totalsamples > args.samples
        )
        payload_thread = Thread(target=task_handler.prepare_payloads)
        thread = Thread(target=result_handler.refresh_display)
        task_thread = Thread(target=task_handler.auto_send)
        thread.start()
        payload_thread.start()
        stub = ArmoniKSubmitter(channel)
        task_handler._client = stub
        result_handler.stub = stub
        session_id = stub.create_session(
            TaskOptions(
                max_duration=timedelta(seconds=300),
                priority=1,
                max_retries=5,
                options={"nThreads": "4"},
            )
        )
        print("Session created")
        task_handler.session = session_id
        result_handler.session_id = session_id
        task_thread.start()
        results_status = set()

        count = {
            "Completed": 0,
            "Pending": 0,
            "Processing": 0,
            "AvailableResult": 0,
            "ProcessedResult": 0,
        }
        line_length = 0
        try:
            while (
                (
                    not task_handler.done
                    and task_thread.is_alive()
                    or len(task_handler.task_result_mapping) > 0
                )
                or len(results_status) > 0
                or result_handler.to_process_queue.qsize() > 0
            ):
                hasDone = False
                count["Processing"] = 0
                count["Pending"] = 0
                if len(task_handler.task_result_mapping) > 0:
                    statuses = stub.get_task_status(
                        list(task_handler.task_result_mapping.keys())
                    )
                    for task_id, status in statuses.items():
                        if status == TaskStatus.COMPLETED:
                            count["Completed"] += 1
                            results_status.add(
                                task_handler.task_result_mapping[task_id]
                            )
                            hasDone = True
                            task_handler.task_result_mapping.pop(task_id, "")
                        elif (
                            status == TaskStatus.PROCESSING
                            or status == TaskStatus.DISPATCHED
                        ):
                            count["Processing"] += 1
                        elif (
                            status == TaskStatus.CREATING
                            or status == TaskStatus.SUBMITTED
                        ):
                            count["Pending"] += 1
                        elif status == TaskStatus.ERROR:
                            print(f"Task in error : {task_id}")
                if len(results_status) > 0:
                    status_reply = stub._client.GetResultStatus(
                        GetResultStatusRequest(
                            result_ids=list(results_status), session_id=session_id
                        )
                    )
                    for s in status_reply.id_statuses:
                        if s.status == RESULT_STATUS_COMPLETED:
                            result_handler.add_to_queue(s.result_id)
                            count["AvailableResult"] += 1
                            results_status.discard(s.result_id)
                            hasDone = True
                        elif s.status == RESULT_STATUS_ABORTED:
                            print(f"Result in error : {s.result_id}")
                            results_status.discard(s.result_id)
                report = f'\rPrepared: {task_handler.n_prepared}, Pending: {count["Pending"]} Processing: {count["Processing"]} Completed: {count["Completed"]} Available: {count["AvailableResult"]} Processed: {count["AvailableResult"]-result_handler.to_process_queue.qsize()}'
                if len(report) < line_length:
                    print(f'\r{"".join([" "]*line_length)}', end="")
                line_length = len(report)
                print(report, end="")
                time.sleep(0.5)

            result_handler.to_process_queue.join()
        except KeyboardInterrupt as e:
            print()
            print(f"Cancelled by user")
            result_handler.cancelled = True
            task_handler.cancelled = True
            result_handler.done = True
            stub.cancel_session(session_id)
            print("Tasks successfully cancelled")
        except BaseException as e:
            print()
            print(f"Error : {e}")
            result_handler.cancelled = True
            task_handler.cancelled = True
            result_handler.done = True
            stub.cancel_session(session_id)
            print("Tasks successfully cancelled")
        finally:
            try:
                print()
                print("Demo is done !")
                result_handler.done = True
                task_thread.join()
                payload_thread.join()
                while thread.is_alive():
                    thread.join(1)
            except KeyboardInterrupt:
                result_handler.cancelled = True
                thread.join()


if __name__ == "__main__":
    main(parse_args())
