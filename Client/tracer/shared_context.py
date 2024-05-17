import logging
from dataclasses import dataclass, field
from multiprocessing import JoinableQueue, Value
from typing import Any, Callable, Optional

from armonik.common import TaskOptions


def default_cancellation(fut: Any):
    getattr(fut, "cancel", lambda: None)()


@dataclass
class Token:
    fut: Any = None
    is_set: bool = False
    cancellation_function: Callable[[Any], Any] = default_cancellation

    def cancel(self):
        self.is_set = True
        if self.fut is not None:
            self.cancellation_function(self.fut)


@dataclass
class Flag:
    value: Value

    def __bool__(self):
        return self.is_set()

    def is_set(self):
        return self.value.value != 0

    def set(self):
        self.value.value = 1

    def reset(self):
        self.value.value = 0


class SharedContext:
    def __init__(self, params, to_watch_queue:Optional[JoinableQueue] = None, to_retrieve_queue:Optional[JoinableQueue] = None, to_display_queue:Optional[JoinableQueue] = None, finalised_queue:Optional[JoinableQueue] = None):
        self.params = params
        self.to_watch_queue = to_watch_queue if to_watch_queue is not None else JoinableQueue()
        self.to_retrieve_queue = to_retrieve_queue if to_retrieve_queue is not None else JoinableQueue()
        self.to_display_queue = to_display_queue if to_display_queue is not None else JoinableQueue()
        self.finalised_queue = finalised_queue if finalised_queue is not None else JoinableQueue()

    @property
    def server_url(self) -> str:
        return self.params[0]

    @property
    def session_id(self) -> str:
        return self.params[1]

    @property
    def task_options(self):
        return self.params[2]

    @task_options.setter
    def task_options(self, value):
        self.params[2] = value

    @property
    def logging_level(self):
        return self.params[3]

    @property
    def stop_watching_flag(self):
        return self.params[4]

    @property
    def stop_retrieving_flag(self):
        return self.params[5]

    @property
    def stop_display_flag(self):
        return self.params[6]

    @property
    def reset_display_flag(self):
        return self.params[7]

    @reset_display_flag.setter
    def reset_display_flag(self, value):
        self.params[7] = value

    @stop_display_flag.setter
    def stop_display_flag(self, value):
        self.params[6] = value

    @stop_retrieving_flag.setter
    def stop_retrieving_flag(self, value):
        self.params[5] = value

    @stop_watching_flag.setter
    def stop_watching_flag(self, value):
        self.params[4] = value

    @session_id.setter
    def session_id(self, value):
        self.params[1] = value
