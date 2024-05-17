import logging
import time
from queue import Empty
from threading import Thread
from traceback import format_exception
from typing import cast, Tuple
from colorsys import hsv_to_rgb

import cv2
import numpy as np

from tracer.objects import TracerResult
from tracer.shared_context import SharedContext, Token


def color_from_samples(
    n_samples: int,
    min_samples: int,
    max_samples: int,
    from_hue: float = 0.66,
    to_hue: float = 0.0,
) -> Tuple[int, int, int]:
    n_samples = max(min(n_samples, max_samples), min_samples)
    factor = (n_samples - min_samples) / (max_samples - min_samples)
    rgb = hsv_to_rgb(to_hue * factor + from_hue * (1 - factor), 1.0, 1.0)
    return int(rgb[2] * 255), int(rgb[1] * 255), int(rgb[0] * 255)


def display_window(
    ctx: SharedContext,
    window_name: str,
    height: int,
    width: int,
    cancellation_token: Token,
):
    print("Creating window")
    cv2.namedWindow(window_name)
    cv2.setWindowProperty(window_name, cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
    img = np.zeros((height, width, 3), np.uint8)
    max_delay = 1.0 / 30.0
    need_refresh = True
    im = 0
    try:
        while not cancellation_token.is_set:
            if ctx.reset_display_flag:
                print("Reset window")
                img.fill(0)
                ctx.reset_display_flag = 0
                need_refresh = True
            start = time.perf_counter()
            if need_refresh:
                cv2.imshow(window_name, img)
                im += 1
            cv2.waitKey(1)
            need_refresh = False
            current = time.perf_counter()
            try:
                while current - start < max_delay:
                    result = cast(
                        TracerResult,
                        ctx.to_display_queue.get(timeout=max_delay - current + start),
                    )
                    img[
                        height
                        - result.coord_x
                        - result.task_height : height
                        - result.coord_x,
                        result.coord_y : result.coord_y + result.task_width,
                        :,
                    ] = (
                        result.pixels_to_numpy_array()
                        if result.isFinal
                        else cv2.rectangle(
                            result.pixels_to_numpy_array().copy(),
                            [0, 0],
                            [result.task_width - 1, result.task_height - 1],
                            color_from_samples(result.n_samples_per_pixel, 100, 500),
                            1,
                        )
                    )
                    ctx.to_display_queue.task_done()
                    current = time.perf_counter()
                    need_refresh = True
            except Empty:
                pass
        cv2.destroyAllWindows()
    except Exception as e:
        print(
            f"Exception while displaying results : {format_exception(type(e), e, e.__traceback__)}"
        )
    logging.info("Display Exited")


def start_display(height: int, width: int, *ctx):
    ctx = SharedContext(*ctx)
    token = Token()
    thread = Thread(
        target=display_window, args=(ctx, "ArmoniKDemo", height, width, token)
    )
    try:
        thread.start()
        while not ctx.stop_display_flag:
            time.sleep(0.25)
    except KeyboardInterrupt:
        pass
    print("Stopping display...")
    token.cancel()
    thread.join()
    cv2.destroyAllWindows()
