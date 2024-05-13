from tracer.objects import Sphere, Reflection, Camera, Scene

spheres = [
    Sphere(
        1e5,
        [1e5 - 5, 40.8, 81.6],
        [0.0, 0.0, 0.0],
        [0.75, 0.25, 0.25],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5,
        [-1e5 + 104, 40.8, 81.6],
        [0.0, 0.0, 0.0],
        [0.25, 0.25, 0.75],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5, [50, 40.8, 1e5], [0.0, 0.0, 0.0], [0.75, 0.75, 0.75], Reflection.DIFF, -1
    ),
    Sphere(
        1e5,
        [50, 40.8, -1e5 + 170],
        [0.0, 0.0, 0.0],
        [0.0, 0.0, 0.0],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        1e5, [50, 1e5, 81.6], [0.0, 0.0, 0.0], [0.75, 0.75, 0.75], Reflection.DIFF, -1
    ),
    Sphere(
        1e5,
        [50, -1e5 + 81.6, 81.6],
        [0.0, 0.0, 0.0],
        [0.75, 0.75, 0.75],
        Reflection.DIFF,
        -1,
    ),
    Sphere(
        16.5,
        [40, 16.5, 47],
        [0.0, 0.0, 0.0],
        [0.999, 0.999, 0.999],
        Reflection.SPEC,
        -1,
    ),
    Sphere(
        10, [90, 25, 125], [0.0, 0.0, 0.0], [0.999, 0.999, 0.999], Reflection.SPEC, -1
    ),
    Sphere(
        16.5,
        [73, 46.5, 94],
        [0.0, 0.0, 0.0],
        [0.999, 0.999, 0.999],
        Reflection.REFR,
        -1,
    ),
    Sphere(
        10, [15, 45, 112], [0.0, 0.0, 0.0], [0.999, 0.999, 0.999], Reflection.DIFF, -1
    ),
    Sphere(15, [16, 16, 130], [0.0, 0.0, 0.0], [0, 0.999, 0], Reflection.REFR, -1),
    Sphere(7.5, [40, 8, 120], [0.0, 0.0, 0.0], [0.999, 0.999, 0], Reflection.REFR, -1),
    Sphere(8.5, [67, 9, 122], [0.1, 0.1, 0.0], [0.999, 0.999, 0], Reflection.REFR, -1),
    Sphere(10, [80, 12, 92], [0.1, 1, 0.1], [0.1, 0.6, 0.1], Reflection.DIFF, -1),
    Sphere(
        600, [50, 681.33, 81.6], [1.5, 1.5, 1.5], [0.0, 0.0, 0.0], Reflection.DIFF, -1
    ),
    Sphere(9, [95, 65, 81.6], [0.0, 0.4, 0.8], [0.1, 0.3, 0.6], Reflection.DIFF, -1),
    Sphere(8, [15, 70, 75], [1, 0.1, 0.1], [0.6, 0.1, 0.1], Reflection.DIFF, -1),
]

camera = Camera()


def get_scene(
    img_width: int, img_height: int, kill_depth: int, split_depth: int
) -> Scene:
    return Scene(img_width, img_height, kill_depth, split_depth, camera, spheres)
