import armonik.common.submitter_common_pb2 as subcommon
import armonik.common.objects_pb2 as obj
import armonik.client.submitter_service_pb2_grpc as submitter_service
import copy
import uuid


class SessionClient:
    def __init__(self, submitter_client, session_id):
        """
        :param submitter_client: SubmitterClient
        :type submitter_client: SubmitterStub
        :param session_id: id of the session
        :type session_id: str
        """
        self._client = submitter_client
        self._session_id = session_id

    def get_result(self, result_id) -> bytes:
        result_request = obj.ResultRequest(
            result_id=result_id,
            session=self._session_id
        )
        return SubmitterClientExt.get_result(self._client, result_request)
        availability_reply = self._client.WaitForAvailability(result_request)
        ret = availability_reply.WhichOneof("Type")
        if ret is None:
            raise Exception("Error with server")
        elif ret == "ok":
            pass
        elif ret == "error":
            raise Exception("Task in error")
        elif ret ==  "not_completed_task":
            raise Exception("Task not completed")
        else:
            raise Exception("Unknown return type")

        task_output = self._client.TryGetTaskOutput(result_request)
        ret = task_output.WhichOneof("Type")
        if ret is None:
            raise Exception("Error with server")
        elif ret == "ok":
            pass
        elif ret == "error":
            raise Exception("Task in error")
        else:
            raise Exception("Unknown return type")
        response = SubmitterClientExt.get_result(self._client, result_request)
        return response

    def submit_task(self, payload) -> subcommon.CreateTaskReply.TaskInfo:
        return self.submit_tasks([payload])[0]

    def submit_tasks(self, payload_array) -> list[subcommon.CreateTaskReply.TaskInfo]:
        return self.submit_tasks_with_dependencies([(p, []) for p in payload_array])

    def submit_tasks_with_dependencies(self, payload_array) -> list[subcommon.CreateTaskReply.TaskInfo]:
        task_requests = []

        for payload, deps in payload_array:
            req_id = self._session_id + "%" + str(uuid.uuid4())
            task_request = obj.TaskRequest()
            task_request.expected_output_keys.append(req_id)
            for d_id in deps:
                task_request.data_dependencies.append((self._session_id+"%"+d_id))
            task_request.payload = copy.deepcopy(payload)
            task_requests.append(task_request)
        create_tasks_reply = SubmitterClientExt.create_tasks(self._client, self._session_id, None, task_requests)
        ret = create_tasks_reply.WhichOneof("Response")
        if ret is None or ret == "error" :
            raise Exception('Issue with server when submitting tasks')
        if ret == "":
            raise Exception(f'Non successful ids {create_tasks_reply.non_successfull_ids}')
        elif ret == "creation_status_list":
            tasks_created = [creation_status.task_info for creation_status in create_tasks_reply.creation_status_list.creation_statuses if creation_status.WhichOneof("Status") == "task_info"]
            print(f'{len(tasks_created)} out of {len(payload_array)} tasks have been created')
        else:
            raise Exception("Unknown value")
        return tasks_created

    def wait_for_completion(self, task_id):
        wait_request = submitter_service.WaitRequest(
            Filter=submitter_service.TaskFilter(
                task=submitter_service.TaskFilter.IdsRequest()
            ),
            stop_on_first_task_error=True,
            stop_on_first_task_cancellation=True
        )
        wait_request.Filter.task.ids.append(task_id)
        self._client.WaitForCompletion(wait_request)

    def wait_for_completion_async(self, task_id):
        wait_request = submitter_service.WaitRequest(
            Filter=submitter_service.TaskFilter(
                task=submitter_service.TaskFilter.IdsRequest()
            ),
            stop_on_first_task_error=True,
            stop_on_first_task_cancellation=True
        )
        wait_request.Filter.task.ids.append(task_id)
        return self._client.WaitForCompletion.future(wait_request)

    def get_status(self, task_id):
        return self._client.GetStatus(subcommon.GetTaskStatusRequest(task_id=task_id)).status

    def get_statuses(self, tasks_infos):
        req = subcommon.GetTaskStatusRequest()
        for t in tasks_infos:
            req.task_ids.append(t)
        return self._client.GetTaskStatus(req).id_statuses

    def get_result_statuses(self, result_ids):
        req = subcommon.GetResultStatusRequest(session_id = self._session_id)
        for r in result_ids:
            req.result_ids.append(r)
        return self._client.GetResultStatus(req).id_statuses

    def cancel_tasks(self, task_ids):
        tf = subcommon.TaskFilter()
        for i in task_ids:
            tf.task.ids.append(i)
        #tf.excluded.Statuses.append(task_status_pb2.TASK_STATUS_CANCELED)
        self._client.CancelTasks(tf)


class SubmitterClientExt:

    @staticmethod
    async def get_result_async(client, result_request) -> bytearray:
        """
                :param client: SubmitterClient
                :type client: SubmitterStub
                :param result_request: id of the session
                :type result_request: objects_pb2.ResultRequest
                """
        streaming_call = client.TryGetResultStream(result_request)
        result = bytearray()
        for message in streaming_call:
            ret = message.WhichOneof("Type")
            if ret is None:
                raise Exception("Error with server")
            elif ret == "result":
                if message.result.WhichOneof("Type") == "data":
                    print(type(message.result.data))
                    result += message.result.data
            elif ret == "error":
                raise Exception("Task in error")
            else:
                raise Exception("Unknown return type")
        return result

    @staticmethod
    def get_result(client, result_request) -> bytearray:
        """
                :param client: SubmitterClient
                :type client: SubmitterStub
                :param result_request: id of the session
                :type result_request: objects_pb2.ResultRequest
                """
        streaming_call = client.TryGetResultStream(result_request)
        result = bytearray()
        for message in streaming_call:
            ret = message.WhichOneof("type")
            if ret is None:
                raise Exception("Error with server")
            elif ret == "result":
                if message.result.WhichOneof("type") == "data":
                    result += message.result.data
            elif ret == "error":
                raise Exception("Task in error")
            else:
                raise Exception("Unknown return type")
        return result

    @staticmethod
    def to_request_stream_internal(request, is_last, chunk_max_size):
        req = subcommon.CreateLargeTaskRequest(
            init_task=obj.InitTaskRequest(
                header=obj.TaskRequestHeader(
                    data_dependencies=copy.deepcopy(request.data_dependencies),
                    expected_output_keys=copy.deepcopy(request.expected_output_keys)
                )
            )
        )
        yield req
        start = 0
        payload_length = len(request.payload)
        if payload_length == 0:
            req = subcommon.CreateLargeTaskRequest(
                task_payload=obj.DataChunk(data=b'')
            )
            yield req
        while start < payload_length:
            chunk_size = min(chunk_max_size, payload_length-start)
            req = subcommon.CreateLargeTaskRequest(
                task_payload=obj.DataChunk(data=copy.deepcopy(request.payload[start:start+chunk_size]))
            )
            yield req
            start += chunk_size
        req = subcommon.CreateLargeTaskRequest(
            task_payload=obj.DataChunk(data_complete=True)
        )
        yield req

        if is_last:
            req = subcommon.CreateLargeTaskRequest(
                init_task=obj.InitTaskRequest(last_task=True)
            )
            yield req

    @staticmethod
    def to_request_stream(requests, session_id, task_options, chunk_max_size):
        req = subcommon.CreateLargeTaskRequest(
            init_request=subcommon.CreateLargeTaskRequest.InitRequest(
                session_id=session_id, task_options=task_options))
        yield req
        if len(requests) == 0:
            return
        for r in requests[:-1]:
            for req in SubmitterClientExt.to_request_stream_internal(r, False, chunk_max_size):
                yield req
        for req in SubmitterClientExt.to_request_stream_internal(requests[-1], True, chunk_max_size):
            yield req

    @staticmethod
    def create_tasks(client, session_id, task_options, task_requests) -> subcommon.CreateTaskReply:
        """
            :param client: SubmitterClient
            :type client: SubmitterStub
            :param task_options: options for the tasks
            :type task_options: objects_pb2.TaskOptions
            :param task_requests
            :type task_requests: list[objects_pb2.TaskRequest]
            """
        configuration = client.GetServiceConfiguration(obj.Empty())
        print(configuration)
        #it = SubmitterClientExt.to_request_stream(task_requests,
        #                                         session_id,
        #                                         task_options,
        #                                         configuration.data_chunk_max_size)
        return client.CreateLargeTasks(SubmitterClientExt.to_request_stream(task_requests,
                                                 session_id,
                                                 task_options,
                                                 configuration.data_chunk_max_size))

    @staticmethod
    def create_small_tasks(client, session_id, task_options, task_requests):
        """
                    :param client: SubmitterClient
                    :type client: SubmitterStub
                    :param task_options: options for the tasks
                    :type task_options: objects_pb2.TaskOptions
                    :param task_requests
                    :type task_requests: list[objects_pb2.TaskRequest]
                    """
        req = submitter_service.CreateSmallTaskRequest(
            session_id=session_id,
            task_options=task_options
        )
        for t in task_requests:
            req.task_requests.append(t)
        return client.CreateSmallTasks(req)


