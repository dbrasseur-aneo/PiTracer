from dataclasses import dataclass
from multiprocessing import JoinableQueue, Value
from typing import Any, Callable

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
    value: Value = Value("b", 0)

    def __bool__(self):
        return self.value != 0

    def set(self):
        self.value.value = 1

    def reset(self):
        self.value.value = 0


@dataclass
class SharedContext:
    server_url: str
    session_id: str
    task_options: TaskOptions
    to_watch_queue = JoinableQueue()
    to_retrieve_queue = JoinableQueue()
    to_display_queue = JoinableQueue()
    finalised_queue = JoinableQueue()
    stop_watching_flag: Flag = Flag()
    stop_retrieving_flag: Flag = Flag()
    stop_display_flag: Flag = Flag()
    reset_display_flag: Flag = Flag()
