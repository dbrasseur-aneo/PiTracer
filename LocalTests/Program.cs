using System.Text.Json;

using ArmoniK.Samples.PiTracer.Adapter;

var payload =
  @"{""img_height"": 320, ""img_width"": 200, ""samples"": 50, ""kill_depth"": 7, ""split_depth"": 4, ""task_width"": 8, ""task_height"": 8, ""coord_x"": 42, ""coord_y"": 43, ""spheres"": [{""radius"": 100000.0, ""position"": [100001.0, 40.8, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.75, 0.25, 0.25], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 100000.0, ""position"": [-99901.0, 40.8, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.25, 0.25, 0.75], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 100000.0, ""position"": [50, 40.8, 100000.0], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.75, 0.75, 0.75], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 100000.0, ""position"": [50, 40.8, -99830.0], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.0, 0.0, 0.0], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 100000.0, ""position"": [50, 100000.0, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.75, 0.75, 0.75], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 100000.0, ""position"": [50, -99918.4, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.75, 0.75, 0.75], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 16.5, ""position"": [40, 16.5, 47], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0.999], ""reflection"": 1, ""max_reflectivity"": -1}, {""radius"": 16.5, ""position"": [73, 46.5, 88], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0.999], ""reflection"": 2, ""max_reflectivity"": -1}, {""radius"": 10, ""position"": [15, 45, 112], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0.999], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 15, ""position"": [16, 16, 130], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0], ""reflection"": 2, ""max_reflectivity"": -1}, {""radius"": 7.5, ""position"": [40, 8, 120], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0], ""reflection"": 2, ""max_reflectivity"": -1}, {""radius"": 8.5, ""position"": [60, 9, 110], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.999, 0.999, 0], ""reflection"": 2, ""max_reflectivity"": -1}, {""radius"": 10, ""position"": [80, 12, 92], ""emission"": [0.0, 0.0, 0.0], ""color"": [0, 0.999, 0], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 600, ""position"": [50, 681.33, 81.6], ""emission"": [12, 12, 12], ""color"": [0.0, 0.0, 0.0], ""reflection"": 0, ""max_reflectivity"": -1}, {""radius"": 5, ""position"": [50, 75, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0, 0.682, 0.999], ""reflection"": 0, ""max_reflectivity"": -1}]}";
Console.WriteLine(payload);
var tracer = JsonSerializer.Deserialize<TracerPayload>(payload);
var sphere =
  JsonSerializer.Deserialize<Sphere>(@"{""radius"": 100000.0, ""position"": [100001.0, 40.8, 81.6], ""emission"": [0.0, 0.0, 0.0], ""color"": [0.75, 0.25, 0.25], ""reflection"": 0, ""max_reflectivity"": -1}");
var pos = JsonSerializer.Deserialize<IList<double>>("[100001.0, 40.8, 81.6]");
var options = new ParallelOptions
              {
                MaxDegreeOfParallelism = 4,
              };
Parallel.For(0,
             tracer.TaskHeight * tracer.TaskWidth,
             options,
             offset =>
             {
               var i = tracer.CoordX + offset / tracer.TaskWidth;
               var j = tracer.CoordY + offset % tracer.TaskWidth;
               Console.WriteLine($"offset {offset} i {i} j {j}");
             });
return 0;
