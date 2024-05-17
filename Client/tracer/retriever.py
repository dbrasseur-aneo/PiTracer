import logging
from queue import Empty
from traceback import format_exception

import grpc

from tracer.objects import TracerResult
from tracer.shared_context import SharedContext
from armonik.client.results import ArmoniKResults


def retrieve_finished_result(ctx: SharedContext, result_id: str) -> bool:
    try:
        with grpc.insecure_channel(ctx.server_url) as channel:
            result = TracerResult(
                ArmoniKResults(channel).download_result_data(result_id, ctx.session_id)
            )
            if not result.isFinal:
                ctx.to_watch_queue.put(result.nextResultId)
            ctx.to_display_queue.put(result)
            ctx.finalised_queue.put((result.coord_x, result.coord_y, result.isFinal))
            return True
    except Exception as e:
        logging.error(
            f"Exception while retrieving results : {format_exception(type(e), e, e.__traceback__)}"
        )
    return False


def start_retriever(*ctx):
    print("Started retrieving")
    ctx = SharedContext(*ctx)
    try:
        while not ctx.stop_retrieving_flag:
            try:
                result_id = ctx.to_retrieve_queue.get(timeout=1.0)
                if not retrieve_finished_result(ctx, result_id):
                    ctx.to_retrieve_queue.put(result_id)
                ctx.to_retrieve_queue.task_done()
            except Empty:
                pass
    except KeyboardInterrupt:
        pass
    logging.info("Retriever Exited")
