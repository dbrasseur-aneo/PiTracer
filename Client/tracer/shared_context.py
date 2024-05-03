from dataclasses import dataclass
from multiprocessing import JoinableQueue, Value
from typing import Any


@dataclass
class Token:
    fut: Any = None
    is_set: bool = False

    def cancel(self):
        self.is_set = True
        if self.fut is not None:
            getattr(self.fut, "cancel", lambda: None)()


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
    to_watch_queue = JoinableQueue()
    to_retrieve_queue = JoinableQueue()
    to_display_queue = JoinableQueue()
    finalised_queue = JoinableQueue()
    stop_watching_flag: Flag = Flag()
    stop_retrieving_flag: Flag = Flag()
    stop_display_flag: Flag = Flag()
    reset_display_flag: Flag = Flag()
