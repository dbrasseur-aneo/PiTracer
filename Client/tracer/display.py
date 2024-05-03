import time
from queue import Empty
from threading import Thread

import cv2
import numpy as np

from tracer.shared_context import SharedContext, Token


def display_window(
    ctx: SharedContext,
    window_name: str,
    height: int,
    width: int,
    cancellation_token: Token,
):
    cv2.namedWindow(window_name, cv2.WINDOW_FULLSCREEN)
    img = np.zeros((height, width, 3), np.uint8)
    max_delay = 1.0 / 30.0
    need_refresh = True
    try:
        while not cancellation_token.is_set:
            if ctx.reset_display_flag:
                img.fill(0)
                ctx.reset_display_flag.reset()
            start = time.perf_counter()
            if need_refresh:
                cv2.imshow(window_name, img)
            cv2.waitKey(1)
            need_refresh = False
            current = time.perf_counter()
            try:
                while current - start < max_delay:
                    result = ctx.to_display_queue.get(
                        timeout=max_delay - current + start
                    )
                    img[
                        height
                        - result.coord_x
                        - result.task_height : height
                        - result.coord_x,
                        result.coord_y : result.coord_y + result.task_width,
                        :,
                    ] = result.pixels_to_numpy_array()
                    current = time.perf_counter()
                    need_refresh = True
            except Empty:
                pass
    except Exception as e:
        print(f"Exception: {e}")


def start_display(ctx: SharedContext, height: int, width: int):
    token = Token()
    thread = Thread(
        target=display_window, args=(ctx, "ArmoniKDemo", height, width, token)
    )
    thread.start()
    while not ctx.stop_display_flag:
        time.sleep(0.25)
    token.cancel()
    thread.join()
