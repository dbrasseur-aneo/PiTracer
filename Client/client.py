# -*- coding: utf-8 -*-
import argparse
import grpc
import numpy as np
import json
import google.protobuf.duration_pb2

import armonik.client.submitter_service_pb2_grpc as sub
import armonik.common.task_status_pb2 as task_status
import armonik.common.result_status_pb2 as result_status
import armonik.client.submitter_service_pb2_grpc as obj
import armonik.common.submitter_common_pb2 as subcommon
from client_wrapper import *
import base64
import math
import random
import cv2
from threading import Thread
from queue import Queue, Empty
import time


class Reflection:
	DIFF = 0
	SPEC = 1
	REFR = 2


class Sphere:
	def __init__(self, radius=0.0, position=[0.0, 0.0, 0.0], emission=[0.0, 0.0, 0.0], color=[0.0, 0.0, 0.0],
				 reflection=Reflection.DIFF, max_reflectivity=-1.0):
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
		self.camera = {}


class TracerResult:
	def __init__(self, result):
		self.coord_x = int.from_bytes(result[0:4], 'little')
		self.coord_y = int.from_bytes(result[4:8], 'little')
		self.task_width = int.from_bytes(result[8:12], 'little')
		self.task_height = int.from_bytes(result[12:16], 'little')
		self.pixels = list(result[16:])

	def pixels_to_numpy_array(self) -> np.array:
		return np.flip(np.array(self.pixels, np.uint8).reshape((self.task_height, self.task_width, 3)), axis=0)


class Camera:
	def __init__(self, length=140, cst=0.5135, position=None, direction=None):
		if position is None:
			position = [50, 52, 295.6]
		if direction is None:
			direction = [0, -0.042612, -1]
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
	parser.add_argument('--samples', help='number of samples per task', default=200, type=int)
	parser.add_argument('--totalsamples', help='minimum number of samples per pixel', default=200, type=int)
	parser.add_argument('--killdepth', help='ray kill depth', default=7, type=int)
	parser.add_argument('--splitdepth', help='ray split depth', default=0, type=int)
	parser.add_argument('--taskheight', help='height of a task in pixels', default=32, type=int)
	parser.add_argument('--taskwidth', help="width of a task in pixels", default=32, type=int)
	return parser.parse_args()


spheres = \
	[Sphere(1e5, [1e5 + 1, 40.8, 81.6], [0.0, 0.0, 0.0], [.75, .25, .25], Reflection.DIFF, -1),
		Sphere(1e5, [-1e5 + 99, 40.8, 81.6], [0.0, 0.0, 0.0], [.25, .25, .75], Reflection.DIFF, -1),
		Sphere(1e5, [50, 40.8, 1e5], [0.0, 0.0, 0.0], [.75, .75, .75], Reflection.DIFF, -1),
		Sphere(1e5, [50, 40.8, -1e5 + 170], [0.0, 0.0, 0.0], [0.0, 0.0, 0.0], Reflection.DIFF, -1),
		Sphere(1e5, [50, 1e5, 81.6], [0.0, 0.0, 0.0], [0.75, .75, .75], Reflection.DIFF, -1),
		Sphere(1e5, [50, -1e5 + 81.6, 81.6], [0.0, 0.0, 0.0], [0.75, .75, .75], Reflection.DIFF, -1),
		Sphere(16.5, [40, 16.5, 47], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.SPEC, -1),
		Sphere(16.5, [73, 46.5, 88], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.REFR, -1),
		Sphere(10, [15, 45, 112], [0.0, 0.0, 0.0], [.999, .999, .999], Reflection.DIFF, -1),
		Sphere(15, [16, 16, 130], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
		#Sphere(7.5, [40, 8, 120], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
		#Sphere(8.5, [60, 9, 110], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
		Sphere(10, [80, 12, 92], [0.0, 0.0, 0.0], [0, .999, 0], Reflection.DIFF, -1),
		Sphere(600, [50, 681.33, 81.6], [1, 1, 1], [0.0, 0.0, 0.0], Reflection.DIFF, -1),
		Sphere(5, [50, 75, 81.6], [0.0, 0.0, 0.0], [0, .682, .999], Reflection.DIFF, -1)]

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


def from_bytes(payload):
	return json.loads(base64.b64decode(payload))


def create_session(stub):
	session_id = str(uuid.uuid4())
	request = subcommon.CreateSessionRequest(
		default_task_option=obj.TaskOptions(
			max_retries=2,
			max_duration=google.protobuf.duration_pb2.Duration(seconds=300),
			priority=1,
			options={"nThreads": "8"}))
	return SessionClient(stub, stub.CreateSession(request).session_id)


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
	def __init__(self, session, stub, total_height, total_width):
		self.img = np.zeros((total_height, total_width, 3), np.uint8)
		self.session = session
		self.stub = stub
		self.done = False
		self.imwidth = total_width
		self.imheight = total_height
		self.need_refresh = False
		self.to_process_queue = Queue()
		self.task_future_mapping = {}
		self.task_done={}
		self.cancelled = False
		self.in_progress = []

	def refresh_display(self):
		cv2.namedWindow("Display")
		while (not self.cancelled) and ((not self.done) or self.need_refresh):
			self.need_refresh = False
			cv2.imshow("Display", self.img)
			cv2.waitKey(16)
		if not self.cancelled:
			cv2.waitKey(0)

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
		chunk = self.img[
			self.imheight-result.coord_x - result.task_height:self.imheight-result.coord_x,
			result.coord_y:result.coord_y + result.task_width,
			:]
		self.img[
			self.imheight - result.coord_x - result.task_height:self.imheight - result.coord_x,
			result.coord_y:result.coord_y + result.task_width,
			:] = (np.power((n_times * np.power(chunk.astype(float)/255, 2.2) + np.power(result.pixels_to_numpy_array().astype(float)/255, 2.2))/(n_times+1), 1/2.2)*255+0.5).astype(np.uint8)
		self.task_done[coords] = n_times+1

	def process(self, task_id):
		result = self.session.get_result(task_id)
		result = TracerResult(result)
		self.copy_to_img(result)
		self.need_refresh = True
		return result

	def async_process(self, fut):
		self.to_process_queue.put(self.task_future_mapping[fut])
		del self.task_future_mapping[fut]

	def process_and_wait(self, task_id):
		self.session.wait_for_completion(task_id)
		self.process(task_id)

	def as_completed(self, task_ids):
		for t in task_ids:
			fut = self.session.wait_for_completion_async(t)
			fut.add_done_callback(self.async_process)
			self.task_future_mapping[fut] = t
		while (not self.to_process_queue.empty()) or len(self.task_future_mapping) > 0:
			task_id = None
			try:
				task_id = self.to_process_queue.get(timeout=1)
			except Empty:
				pass
			if task_id is not None:
				self.process(task_id)


def main(args):
	print("Hello PiTracer Demo !")
	if args.server_url is None:
		print("server url is mandatory")
		return
	with grpc.insecure_channel(args.server_url) as channel:
		print("GRPC channel started")
		stub = sub.SubmitterStub(channel)
		session_client = create_session(stub)
		print("Session created")
		result_handler = ResultHandler(session_client, stub, args.height, args.width)
		thread = Thread(target=result_handler.refresh_display)
		payloads = get_payloads(args)
		print("Payloads created")
		packet_index = 0
		packet_size = 128
		next_batch_thres=32
		task_infos = session_client.submit_tasks(payloads[packet_index:packet_index+packet_size])
		current_packet = dict([(t.task_id, t.expected_output_keys[0])for t in task_infos])
		results_status = {}
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
				count["AvailableResult"]=0
				if len(current_packet) > 0:
					statuses = session_client.get_statuses(current_packet)
					for status in statuses:
						if status.status == task_status.TASK_STATUS_COMPLETED:
							count["Completed"] += 1
							results_status[current_packet[status.task_id]] = None
							hasDone = True
							if(current_packet.pop(status.task_id, "KeyNotFound") == "KeyNotFound"):
								print("Removing unknown task from packet")
						elif status.status == task_status.TASK_STATUS_PROCESSING or status.status == task_status.TASK_STATUS_DISPATCHED:
							count["Processing"] += 1
						elif status.status == task_status.TASK_STATUS_CREATING or status.status == task_status.TASK_STATUS_SUBMITTED:
							count["Pending"] += 1
						elif status.status == task_status.TASK_STATUS_ERROR:
							print(f'Task in error : {status.task_id}')
							if(current_packet.pop(status.task_id, "KeyNotFound") == "KeyNotFound"):
								print("Removing unknown task from packet")
					if len(current_packet) < next_batch_thres and packet_index < len(payloads):
						current_packet.update(dict([(t.task_id, t.expected_output_keys[0]) for t in session_client.submit_tasks(payloads[packet_index:packet_index+packet_size])]))
						packet_index += packet_size
				if len(results_status) > 0:
					statuses = session_client.get_result_statuses(results_status)
					for status in statuses:
						if status.status == result_status.RESULT_STATUS_COMPLETED:
							hasDone = True
							count["ProcessedResult"] += 1
							result_handler.process(status.result_id)
							if(results_status.pop(status.result_id, "KeyNotFound") == "KeyNotFound"):
								print("Removing unknown result from list")
				print(f'Task statuses : \n Pending : {count["Pending"]}\n Processing : {count["Processing"]}\n Completed : {count["Completed"]}\n Processed results : {count["ProcessedResult"]}\n')
				if not hasDone:
					print("Sleeping...")
					time.sleep(1)
			print("Demo is done !")
		except BaseException as e:
			print(f'Cancelled : {str(e)}')
			result_handler.cancelled = True
			result_handler.done = True
			session_client.cancel_tasks(current_packet)
			print("Tasks successfully canceled")
		finally:
			result_handler.done = True
			thread.join()
	cv2.destroyAllWindows()


if __name__ == '__main__':
	main(parse_args())
