from dataclasses import dataclass, field
from typing import List

import numpy as np


class Reflection:
    DIFF = 0
    SPEC = 1
    REFR = 2


class Sphere:
    def __init__(
        self,
        radius=0.0,
        position=None,
        emission=None,
        color=None,
        reflection=Reflection.DIFF,
        max_reflectivity=-1.0,
    ):
        if color is None:
            color = [0.0, 0.0, 0.0]
        if emission is None:
            emission = [0.0, 0.0, 0.0]
        if position is None:
            position = [0.0, 0.0, 0.0]
        self.radius = radius
        self.position = position
        self.emission = emission
        self.color = color
        self.reflection = reflection
        self.max_reflectivity = max_reflectivity

    def to_bytes(self):
        return (
            np.array(
                [
                    self.radius,
                    self.position[0],
                    self.position[1],
                    self.position[2],
                    self.emission[0],
                    self.emission[1],
                    self.emission[2],
                    self.color[0],
                    self.color[1],
                    self.color[2],
                ],
                dtype=np.float32,
            ).tobytes()
            + self.reflection.to_bytes(4, "little")
            + np.array([self.max_reflectivity], dtype=np.float32).tobytes()
        )


class TracerPayload:
    def __init__(self, args, coord_x=0, coord_y=0):
        self.img_height = args.height
        self.img_width = args.width
        self.samples = args.samples
        self.kill_depth = args.killdepth
        self.split_depth = args.splitdepth
        self.task_width = args.taskwidth
        self.task_height = args.taskheight
        self.coord_x = coord_x
        self.coord_y = coord_y
        self.spheres = []
        self.camera = Camera()

    def to_byte(self):
        pb = []
        pb.extend(self.img_width.to_bytes(4, "little"))
        pb.extend(self.img_height.to_bytes(4, "little"))
        pb.extend(self.coord_x.to_bytes(4, "little"))
        pb.extend(self.coord_y.to_bytes(4, "little"))
        pb.extend(self.kill_depth.to_bytes(4, "little"))
        pb.extend(self.split_depth.to_bytes(4, "little"))
        pb.extend(self.task_width.to_bytes(4, "little"))
        pb.extend(self.task_height.to_bytes(4, "little"))
        pb.extend(self.samples.to_bytes(4, "little"))
        pb.extend(self.camera.to_bytes())
        for s in self.spheres:
            pb.extend(s.to_bytes())
        return bytes(pb)


class TracerResult:
    def __init__(self, result: bytes):
        self.coord_x = int.from_bytes(result[0:4], "little")
        self.coord_y = int.from_bytes(result[4:8], "little")
        self.task_width = int.from_bytes(result[8:12], "little")
        self.task_height = int.from_bytes(result[12:16], "little")
        self.n_samples_per_pixel = int.from_bytes(result[16:20], "little")
        self.isFinal = int.from_bytes(result[20:24], "little")
        pixel_offset = 24
        pixels_size = self.task_height * self.task_width * 3
        samples_offset = pixel_offset + pixels_size
        samples_size = self.task_height * self.task_width * 3 * 4
        next_result_id_offset = samples_offset + samples_size
        self.pixels = list(result[pixel_offset:samples_offset])
        self.samples = list(result[samples_offset:next_result_id_offset])
        self.nextResultId = result[next_result_id_offset:].decode("ascii")

    def pixels_to_numpy_array(self) -> np.ndarray:
        return np.flip(
            np.array(self.pixels, np.uint8).reshape(
                (self.task_height, self.task_width, 3)
            ),
            axis=0,
        )


@dataclass
class Payload:
    coord_x: int
    coord_y: int
    task_width: int
    task_height: int
    samples: int

    def to_bytes(self) -> bytes:
        pb = []
        pb.extend(self.coord_x.to_bytes(4, "little"))
        pb.extend(self.coord_y.to_bytes(4, "little"))
        pb.extend(self.task_width.to_bytes(4, "little"))
        pb.extend(self.task_height.to_bytes(4, "little"))
        pb.extend(self.samples.to_bytes(4, "little"))
        return bytes(pb)


@dataclass
class Camera:
    length: float = 140
    cst: float = 0.4635
    position: List[float] = field(default_factory=lambda: [50, 52, 295.6])
    direction: List[float] = field(default_factory=lambda: [0, -0.072612, -1])

    def to_bytes(self) -> bytes:
        return np.array(
            [
                self.length,
                self.cst,
                self.position[0],
                self.position[1],
                self.position[2],
                self.direction[0],
                self.direction[1],
                self.direction[2],
            ],
            dtype=np.float32,
        ).tobytes()


@dataclass
class Scene:
    img_width: int
    img_height: int
    kill_depth: int
    split_depth: int
    camera: Camera
    spheres: List[Sphere]

    def to_bytes(self) -> bytes:
        pb = []
        pb.extend(self.img_width.to_bytes(4, "little"))
        pb.extend(self.img_height.to_bytes(4, "little"))
        pb.extend(self.kill_depth.to_bytes(4, "little"))
        pb.extend(self.split_depth.to_bytes(4, "little"))
        pb.extend(self.camera.to_bytes())
        for s in self.spheres:
            pb.extend(s.to_bytes())
        return bytes(pb)
