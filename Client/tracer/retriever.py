import logging
from concurrent.futures import ThreadPoolExecutor
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
        logging.error(f"Exception while retrieving results : {format_exception(type(e), e, e.__traceback__)}")
    return False


def start_retriever(*ctx):
    ctx = SharedContext(*ctx)
    while not ctx.stop_retrieving_flag:
        try:
            result_id = ctx.to_retrieve_queue.get(timeout=0.25)
            #logging.info(f"Need to retrieve result {result_id}")
            if not retrieve_finished_result(ctx, result_id):
                ctx.to_retrieve_queue.put(result_id)
            ctx.to_retrieve_queue.task_done()
        except Empty:
            pass
    # with ThreadPoolExecutor(max_workers=4) as executor:
    #     while not ctx.stop_retrieving_flag:
    #         try:
    #             result_id = ctx.to_retrieve_queue.get(timeout=0.25)
    #             executor.submit(retrieve_finished_result, ctx, result_id).add_done_callback(
    #                 lambda _: ctx.to_retrieve_queue.task_done())
    #         except Empty:
    #             pass
    logging.info("Retriever Exited")
