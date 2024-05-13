from multiprocessing import Queue
from queue import Empty
from threading import Thread

import grpc

from tracer.shared_context import SharedContext, Token
from armonik.common import ResultStatus
from armonik.protogen.client.events_service_pb2_grpc import EventsStub
from armonik.protogen.common.events_common_pb2 import (
    EventSubscriptionRequest,
    EVENTS_ENUM_RESULT_STATUS_UPDATE,
)


def watch_finished_results(
    ctx: SharedContext, out_queue: Queue, cancellation_token: Token
):
    try:
        with grpc.insecure_channel(ctx.server_url) as channel:
            subscription = EventsStub(channel).GetEvents(
                EventSubscriptionRequest(
                    session_id=ctx.session_id,
                    returned_events=[EVENTS_ENUM_RESULT_STATUS_UPDATE],
                )
            )
            cancellation_token.fut = subscription
            for e in subscription:
                result_id: str = e.result_status_update.result_id
                result_status: ResultStatus = e.result_status_update.result_status
                if result_status in [ResultStatus.COMPLETED, ResultStatus.ABORTED]:
                    out_queue.put((result_id, result_status))
    except Exception as e:
        print(f"Exception: {e}")


def start_watcher(ctx: SharedContext):
    q = Queue()
    token = Token()
    thread = Thread(target=watch_finished_results, args=(ctx, q, token))
    thread.start()
    followed_tasks = {}
    while not ctx.stop_watching_flag:
        try:
            while True:
                result_id = ctx.to_watch_queue.get(timeout=0.25)
                status = followed_tasks.setdefault(result_id, ResultStatus.UNSPECIFIED)
                if status == ResultStatus.COMPLETED:
                    ctx.to_retrieve_queue.put(result_id)
                ctx.to_watch_queue.task_done()
        except Empty:
            pass
        try:
            while True:
                result_id, result_status = q.get(timeout=0.25)
                old_status = followed_tasks.get(result_id, None)
                followed_tasks[result_id] = result_status
                if (
                    result_status == ResultStatus.COMPLETED
                    and old_status != ResultStatus.COMPLETED
                ):
                    ctx.to_retrieve_queue.put(result_id)
        except Empty:
            pass
    token.cancel()
    thread.join()
