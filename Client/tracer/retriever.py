from queue import Empty

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
            if result.isFinal:
                ctx.finalised_queue.put((result.coord_x, result.coord_y))
            else:
                ctx.to_watch_queue.put(result.nextTaskId)
            ctx.to_display_queue.put(result)
            return True
    except Exception as e:
        print(f"Unable to retrieve result : {e}")
    return False


def start_retriever(ctx: SharedContext):
    while not ctx.stop_retrieving_flag:
        try:
            result_id = ctx.to_retrieve_queue.get(timeout=0.25)
            if not retrieve_finished_result(ctx, result_id):
                ctx.to_retrieve_queue.put(result_id)
            ctx.to_retrieve_queue.task_done()
        except Empty:
            pass
