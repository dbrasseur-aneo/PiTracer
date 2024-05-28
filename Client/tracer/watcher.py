from multiprocessing import Queue
from queue import Empty
from threading import Thread
import time
from typing import cast

from grpc import insecure_channel
from armonik.client import ArmoniKResults
from armonik.common.filter import DurationFilter
from armonik.protogen.common.results_fields_pb2 import (
    ResultField,
    ResultRawField,
    RESULT_RAW_ENUM_FIELD_SESSION_ID,
    RESULT_RAW_ENUM_FIELD_CREATED_AT,
    RESULT_RAW_ENUM_FIELD_RESULT_ID,
)
from armonik.protogen.common.results_filters_pb2 import Filters, FiltersAnd, FilterField

from tracer.shared_context import SharedContext, Token
from armonik.common import ResultStatus, StringFilter
from armonik.protogen.client.events_service_pb2_grpc import EventsStub
from armonik.protogen.common.events_common_pb2 import (
    EventSubscriptionRequest,
    EVENTS_ENUM_RESULT_STATUS_UPDATE,
)

from traceback import format_exception

RESULT_SESSION_FILTER = StringFilter(
    ResultField(
        result_raw_field=ResultRawField(field=RESULT_RAW_ENUM_FIELD_SESSION_ID)
    ),
    Filters,
    FiltersAnd,
    FilterField,
)
RESULT_ID_FILTER = StringFilter(
    ResultField(result_raw_field=ResultRawField(field=RESULT_RAW_ENUM_FIELD_RESULT_ID)),
    Filters,
    FiltersAnd,
    FilterField,
)
RESULT_CREATED_AT_FILTER = DurationFilter(
    ResultField(
        result_raw_field=ResultRawField(field=RESULT_RAW_ENUM_FIELD_CREATED_AT)
    ),
    Filters,
    FiltersAnd,
    FilterField,
)


def watch_finished_results(
    ctx: SharedContext, out_queue: Queue, cancellation_token: Token
):
    while not ctx.stop_watching_flag:
        try:

            with insecure_channel(ctx.server_url) as channel:
                subscription = EventsStub(channel).GetEvents(
                    EventSubscriptionRequest(
                        session_id=ctx.session_id,
                        returned_events=[EVENTS_ENUM_RESULT_STATUS_UPDATE],
                    )
                )
                cancellation_token.fut = subscription
                for e in subscription:
                    result_id: str = e.result_status_update.result_id
                    result_status: ResultStatus = e.result_status_update.status
                    if result_status in [ResultStatus.COMPLETED, ResultStatus.ABORTED]:
                        out_queue.put((result_id, result_status))
        except Exception as e:
            disp = "\n".join(format_exception(type(e), e, e.__traceback__))
            if "Locally cancelled by application!" not in disp:
                print(f"Exception while watching results : {disp}")


def poll_results(
    ctx: SharedContext, out_queue: Queue, cancellation_token: Token, tasks: dict
):
    _ = cancellation_token
    batch_size = 100
    while not ctx.stop_watching_flag:
        try:
            with insecure_channel(ctx.server_url) as channel:
                client = ArmoniKResults(channel)
                i = 0
                n = 1
                while n > i * batch_size and not ctx.stop_watching_flag:
                    n, results = client.list_results(
                        cast(StringFilter, RESULT_SESSION_FILTER == ctx.session_id),
                        i,
                        batch_size,
                        RESULT_ID_FILTER,
                    )
                    for r in results:
                        if r.status in [
                            ResultStatus.COMPLETED,
                            ResultStatus.ABORTED,
                        ] and tasks.get(r.result_id) not in [
                            ResultStatus.COMPLETED,
                            ResultStatus.ABORTED,
                        ]:
                            out_queue.put((r.result_id, r.status))
                    i += 1
        except Exception as e:
            disp = "\n".join(format_exception(type(e), e, e.__traceback__))
            if "Locally cancelled by application!" not in disp:
                print(f"Exception while watching results : {disp}")
        if not ctx.stop_watching_flag:
            time.sleep(3.0)


def start_watcher(use_polling: bool, *ctx):
    print("Started watching")
    ctx = SharedContext(*ctx)
    q = Queue()
    token = Token()
    followed_tasks = {}
    thread = (
        Thread(target=poll_results, args=(ctx, q, token, followed_tasks))
        if use_polling
        else Thread(target=watch_finished_results, args=(ctx, q, token))
    )
    thread.start()
    try:
        while not ctx.stop_watching_flag:
            try:
                while not ctx.stop_watching_flag:
                    result_id = ctx.to_watch_queue.get(timeout=0.25)
                    status = followed_tasks.setdefault(
                        result_id, ResultStatus.UNSPECIFIED
                    )
                    if status == ResultStatus.COMPLETED:
                        ctx.to_retrieve_queue.put(result_id)
                    ctx.to_watch_queue.task_done()
            except Empty:
                pass
            try:
                while not ctx.stop_watching_flag:
                    result_id, result_status = q.get(timeout=0.01)
                    old_status = followed_tasks.get(result_id, None)
                    followed_tasks[result_id] = result_status
                    if result_status == ResultStatus.ABORTED:
                        print("ABORTED RESULT")
                        raise KeyboardInterrupt()
                    if result_status == ResultStatus.COMPLETED and old_status not in [
                        ResultStatus.COMPLETED,
                        None,
                    ]:
                        ctx.to_retrieve_queue.put(result_id)
            except Empty:
                pass
    except KeyboardInterrupt:
        pass
    print("Stopping watcher...")
    token.cancel()
    thread.join()
