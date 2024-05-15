import argparse
import math
import time
from datetime import timedelta
from multiprocessing import Process
from queue import Empty

from armonik.client.results import ArmoniKResults
from armonik.client.sessions import ArmoniKSessions
from armonik.client.tasks import ArmoniKTasks
from armonik.common.objects import TaskOptions, TaskDefinition
from grpc import insecure_channel

from tracer.display import start_display
from tracer.objects import Scene, Payload
from tracer.retriever import start_retriever
from tracer.scene import get_scene
from tracer.shared_context import SharedContext
from tracer.watcher import start_watcher

import logging

logging.basicConfig(level=logging.INFO)


def parse_args():
    parser = argparse.ArgumentParser(description="Client for PiTracer")
    parser.add_argument("--server_url", help="server url", required=True)
    parser.add_argument("--height", help="height of image", default=1080, type=int)
    parser.add_argument("--width", help="width of image", default=1920, type=int)
    parser.add_argument(
        "--samples", help="number of samples per task", default=100, type=int
    )
    parser.add_argument(
        "--error_threshold", help="error threshold", default=5, type=float
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


def create_context(server_url: str, error_threshold: float) -> SharedContext:
    print("Creating context...")
    with insecure_channel(server_url) as channel:
        options = TaskOptions(
            max_duration=timedelta(seconds=300),
            priority=1,
            max_retries=1,
            options={"n_threads": str(4), "errorMetricThreshold": str(error_threshold)},
        )
        print("Context created")
        return SharedContext(
            server_url=server_url,
            session_id=ArmoniKSessions(channel).create_session(options),
            task_options=options,
            logging_level=logging.INFO
        )


def send_scene(context: SharedContext, scene: Scene) -> str:
    print("Sending scene...")
    with insecure_channel(context.server_url) as channel:
        sceneId = (
            ArmoniKResults(channel)
            .create_results({"scene": scene.to_bytes()}, context.session_id)["scene"]
            .result_id
        )
        context.task_options.options["sceneId"] = sceneId
        print("Scene sent")
        return sceneId


def send_payloads(context: SharedContext, payloads: list[Payload]) -> dict[str, str]:
    print("Sending payloads...")
    with insecure_channel(context.server_url) as channel:
        return {
            k: r.result_id
            for k, r in ArmoniKResults(channel)
            .create_results(
                {f"{p.coord_x}_{p.coord_y}": p.to_bytes() for p in payloads},
                context.session_id,
                10,
            )
            .items()
        }


def create_results(context: SharedContext, payloads: list[Payload]) -> dict[str, str]:
    print("Creating results...")
    with insecure_channel(context.server_url) as channel:
        return {
            k: r.result_id
            for k, r in ArmoniKResults(channel)
            .create_results_metadata(
                [f"{p.coord_x}_{p.coord_y}" for p in payloads],
                context.session_id,
                100,
            )
            .items()
        }


def create_task_definitions(
        context: SharedContext,
        scene: str,
        payloads: dict[str, str],
        results: dict[str, str],
) -> dict[str, TaskDefinition]:
    print("Creating task definitions...")
    return {
        k: TaskDefinition(
            payload_id=p,
            payload=b"",
            expected_output_ids=[results[k]],
            data_dependencies=[scene],
        )
        for k, p in payloads.items()
    }


def send_tasks(context: SharedContext, tasks: list[TaskDefinition]) -> None:
    print("Sending tasks...")
    with insecure_channel(context.server_url) as channel:
        ArmoniKTasks(channel).submit_tasks(
            context.session_id, tasks, context.task_options
        )


def dist_from_center(center_x: int, center_y: int, payload: Payload) -> float:
    return (payload.coord_y - center_y) * (payload.coord_y - center_y) + (payload.coord_x - center_x) * (
                payload.coord_x - center_x)


def generate_payloads(
        img_width: int, img_height: int, task_width: int, task_height: int, samples: int
) -> list[Payload]:
    print("Generating payloads...")
    n_rows = int(math.ceil(img_height / task_height))
    n_cols = int(math.ceil(img_width / task_width))
    center_x = img_height // 2 + task_height // 2
    center_y = img_width // 2 + task_width // 2
    return sorted([
        Payload(
            r * task_height,
            c * task_width,
            max(0, min(img_width - c * task_width, task_width)),
            max(0, min(img_height - r * task_height, task_height)),
            samples,
        )
        for r in range(n_rows)
        for c in range(n_cols)
    ], key=lambda p: dist_from_center(center_x, center_y, p))


def abort(ctx: SharedContext, *processes: Process):
    ctx.stop_display_flag.set()
    ctx.stop_retrieving_flag.set()
    ctx.stop_watching_flag.set()
    with insecure_channel(ctx.server_url) as channel:
        ArmoniKSessions(channel).cancel_session(ctx.session_id)
    try:
        for p in processes:
            p.join(0.5)
    except KeyboardInterrupt:
        print("Stopping completely")
        for p in processes:
            p.kill()
        exit(1)
    exit(0)


def main(args):
    print("Hello PiTracer Demo!")
    run_demo = True
    while run_demo:
        context = create_context(args.server_url, args.error_threshold)
        scene_id = send_scene(
            context, get_scene(args.width, args.height, args.killdepth, args.splitdepth)
        )

        display_process = Process(
            target=start_display, args=(args.height, args.width, *context.deconstruct()), daemon=True
        )
        retriever_process = Process(target=start_retriever, args=context.deconstruct(), daemon=True)
        watcher_process = Process(target=start_watcher, args=context.deconstruct(), daemon=True)
        payloads = generate_payloads(
            args.width, args.height, args.taskwidth, args.taskheight, args.samples
        )
        print("Payloads generated")
        expected_finalized_tasks = len(payloads)
        results = create_results(context, payloads)
        print("Results created")
        payload_ids = send_payloads(context, payloads)
        print("Payloads sent")
        try:
            display_process.start()
            retriever_process.start()
            watcher_process.start()
            task_definitions = create_task_definitions(
                context, scene_id, payload_ids, results
            )
            print("Task definitions created")
            for r in results.values():
                context.to_watch_queue.put(r)
            send_tasks(context, list(task_definitions.values()))
            print("Tasks sent")
            current_finalised_tasks = 0
            done_tasks = 0
            total_tasks = expected_finalized_tasks
            start = time.perf_counter()
            while True:
                try:
                    _, _, isFinal = context.finalised_queue.get(timeout=0.20)
                    if isFinal:
                        current_finalised_tasks += 1
                    else:
                        total_tasks += 1
                    done_tasks += 1
                    context.finalised_queue.task_done()
                    end = time.perf_counter()
                    if end - start > 1:
                        start = end
                        print(f"Completion : {done_tasks:04}/{total_tasks:04} ({done_tasks / total_tasks * 100:.1f}%)",
                              end='\r')
                    if current_finalised_tasks >= expected_finalized_tasks:
                        print("\nDemo is done")
                        break
                except Empty:
                    print(f"Completion : {done_tasks:04}/{total_tasks:04} ({done_tasks / total_tasks * 100:.1f}%)",
                          end='\r')
                check = [display_process.is_alive(), watcher_process.is_alive(), retriever_process.is_alive()]
                if not all(check):
                    logging.error(f"Problem running one of the processes {check}")
                    abort(context, display_process, watcher_process, retriever_process)
        except KeyboardInterrupt:
            print("Process aborted")
            abort(context, display_process, watcher_process, retriever_process)
        try:
            run_demo = input("Re-run demo? Y/(N)").lower().strip() == "y"
        except EOFError:
            run_demo = False
        if not run_demo:
            abort(context, display_process, watcher_process, retriever_process)
        else:
            context.reset_display_flag.set()


if __name__ == "__main__":
    main(parse_args())
