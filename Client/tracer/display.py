import logging
import time
import traceback
from queue import Empty
from threading import Thread
from traceback import format_exception

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
    logging.log(ctx.logging_level, "Creating window")
    cv2.namedWindow(window_name)
    cv2.setWindowProperty(window_name, cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
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
                #logging.log(ctx.logging_level, "Displaying window")
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
        cv2.destroyAllWindows()
    except Exception as e:
        print(f"Exception while displaying results : {format_exception(type(e), e, e.__traceback__)}")
    logging.info("Display Exited")


def start_display(height: int, width: int, *ctx):
    ctx = SharedContext(*ctx)
    token = Token()
    thread = Thread(
        target=display_window, args=(ctx, "ArmoniKDemo", height, width, token)
    )
    thread.start()
    while not ctx.stop_display_flag:
        time.sleep(0.25)
    token.cancel()
    thread.join()
