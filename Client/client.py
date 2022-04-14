# -*- coding: utf-8 -*-
import concurrent.futures
import argparse
import grpc
import numpy as np
import json
import google.protobuf.duration_pb2
from client_wrapper import *
import base64
import math
import random
import cv2
from threading import Thread


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


class TracerResult:
	def __init__(self, coord_x=0, coord_y=0, task_width=0, task_height=0, pixels=bytes(), pixels_are_encoded=True):
		self.coord_x = coord_x
		self.coord_y = coord_y
		self.task_width = task_width
		self.task_height = task_height
		if pixels_are_encoded:
			self.pixels = list(bytearray(base64.b64decode(pixels)))
		else:
			self.pixels = list(bytearray(pixels))

	def pixels_to_numpy_array(self) -> np.array:
		return np.array(self.pixels, np.uint8).reshape((self.task_height, self.task_width, 3))


def parse_args():
	parser = argparse.ArgumentParser(description='Client for PiTracer')
	parser.add_argument('--server_url', help='server url')
	parser.add_argument('--height', help='height of image', default=200, type=int)
	parser.add_argument('--width', help='width of image', default=320, type=int)
	parser.add_argument('--samples', help='number of samples', default=50, type=int)
	parser.add_argument('--killdepth', help='ray kill depth', default=7, type=int)
	parser.add_argument('--splitdepth', help='ray split depth', default=4, type=int)
	parser.add_argument('--taskheight', help='height of a task in pixels', default=64, type=int)
	parser.add_argument('--taskwidth', help="width of a task in pixels", default=64, type=int)
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
		Sphere(7.5, [40, 8, 120], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
		Sphere(8.5, [60, 9, 110], [0.0, 0.0, 0.0], [.999, .999, 0], Reflection.REFR, -1),
		Sphere(10, [80, 12, 92], [0.0, 0.0, 0.0], [0, .999, 0], Reflection.DIFF, -1),
		Sphere(600, [50, 681.33, 81.6], [12, 12, 12], [0.0, 0.0, 0.0], Reflection.DIFF, -1),
		Sphere(5, [50, 75, 81.6], [0.0, 0.0, 0.0], [0, .682, .999], Reflection.DIFF, -1)]


def get_payload(args, coord_x, coord_y):
	payload = TracerPayload(args, coord_x, coord_y)
	payload.task_width = max(0, min(payload.img_width - payload.coord_y, payload.task_width))
	payload.task_height = max(0, min(payload.img_height - payload.coord_x, payload.task_height))
	payload.spheres = copy.deepcopy(spheres)
	return payload


def to_byte(payload):
	return base64.b64encode(bytes(json.dumps(payload, default=vars), 'UTF-8'))


def from_bytes(payload):
	return json.loads(base64.b64decode(payload))


def create_session(stub):
	session_id = str(uuid.uuid4())
	request = submitter_service.CreateSessionRequest(
		default_task_option=objects_pb2.TaskOptions(
			MaxDuration=google.protobuf.duration_pb2.Duration(seconds=300),
			MaxRetries=2,
			Priority=1,
			Options={}),
		id=session_id.encode('UTF-8'))
	res = stub.CreateSession(request)
	match res.WhichOneof("result"):
		case None:
			raise Exception("Error with server")
		case "Ok":
			return SessionClient(stub, session_id)
		case "Error":
			raise Exception(f"Error while creating session : {res.Error}")


def get_payloads(args) -> list[bytes]:
	img_height = args.height
	img_width = args.width
	task_width = args.taskwidth
	task_height = args.taskheight
	n_rows = int(math.ceil(img_height/task_height))
	n_cols = int(math.ceil(img_width/task_width))
	coord_list = [(i*task_height, j*task_width) for i in range(n_rows) for j in range(n_cols)]
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

	def refresh_display(self):
		cv2.namedWindow("Display")
		while (not self.done) or self.need_refresh:
			self.need_refresh = False
			cv2.imshow("Display", self.img)
			cv2.waitKey(16)
		cv2.waitKey(0)

	def copy_to_img(self, result):
		"""

		:param result:
		:type result TracerResult
		:return:
		"""
		#print(f'CoordX {result.coord_x} CoordY {result.coord_y} TaskHeight {result.task_height} TaskWidth {result.task_width}')
		self.img[
			result.coord_x:result.coord_x + result.task_height,
			result.coord_y:result.coord_y + result.task_width,
			:] = result.pixels_to_numpy_array()

	def process_result(self, task_id):
		self.session.wait_for_completion(task_id)
		result = self.session.get_result(task_id)
		result = TracerResult(**from_bytes(result))
		self.copy_to_img(result)
		self.need_refresh = True


def main(args):
	if args.server_url is None:
		print("server url is mandatory")
		return
	with grpc.insecure_channel(args.server_url) as channel:
		stub = SubmitterStub(channel)
		session_client = create_session(stub)
		result_handler = ResultHandler(session_client, stub, args.height, args.width)
		thread = Thread(target=result_handler.refresh_display)
		thread.start()
		task_ids = session_client.submit_tasks(get_payloads(args))
		executor = concurrent.futures.ThreadPoolExecutor(16)
		for _ in executor.map(result_handler.process_result, task_ids):
			pass
		result_handler.done = True
		thread.join()
	cv2.destroyAllWindows()


if __name__ == '__main__':
	main(parse_args())
