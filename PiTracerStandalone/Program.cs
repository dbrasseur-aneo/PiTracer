using System.Diagnostics;
using System.Numerics;
using System.Text;

using PiTracerLib;using PiTracerLib.ImageQuality;

const int Height       = 480;
const int Width        = 640;
const int Samples      = 50;
const int TotalSamples = 50;
const int KillDepth    = 7;
const int SplitDepth   = 0;
const int TaskHeight   = 32;
const int TaskWidth    = 32;

var spheres = new[]
              {
                new Sphere(1e5f,  new Vector3(1e5f  + 1,  40.8f,         81.6f), new Vector3(0, 0, 0), new Vector3(.75f, .25f, .25f), Reflection.Diffuse),
                new Sphere(-1e5f, new Vector3(-1e5f + 99, 40.8f, 81.6f), new Vector3(0, 0, 0), new Vector3(.25f, .75f, .75f), Reflection.Diffuse),
                new Sphere(1e5f,  new Vector3(50,         40.8f,         1e5f), new Vector3(0, 0, 0), new Vector3(.75f, .75f, .75f), Reflection.Diffuse),
                new Sphere(1e5f,  new Vector3(50,         40.8f,         -1e5f + 170), new Vector3(0, 0, 0), new Vector3(0.0f, 0.0f, 0.0f), Reflection.Diffuse),
                new Sphere(1e5f,  new Vector3(50,         1e5f,          81.6f), new Vector3(0, 0, 0), new Vector3(0.75f, .75f, .75f), Reflection.Diffuse),
                new Sphere(1e5f,  new Vector3(50,         -1e5f + 81.6f, 81.6f), new Vector3(0, 0, 0), new Vector3(0.75f, .75f, .75f), Reflection.Diffuse),
                new Sphere(16.5f, new Vector3(40,         16.5f,         47), new Vector3(0, 0, 0), new Vector3(.999f, .999f, .999f), Reflection.Specular),
                new Sphere(16.5f, new Vector3(73,         46.5f,         88), new Vector3(0, 0, 0), new Vector3(.999f, .999f, .999f), Reflection.Refraction),
                new Sphere(10f,   new Vector3(15,         45f,           112), new Vector3(0, 0, 0), new Vector3(.999f, .999f, .999f), Reflection.Diffuse),
                new Sphere(15f,   new Vector3(16,         16,            130), new Vector3(0, 0, 0), new Vector3(.999f, .999f, 0), Reflection.Refraction),
                new Sphere(10f,   new Vector3(80,         12,            92), new Vector3(0, 0, 0), new Vector3(0, .999f, 0), Reflection.Diffuse),
                new Sphere(600f,  new Vector3(50,         681.33f,       81.6f), new Vector3(1, 1, 1), new Vector3(0.0f, 0.0f, 0.0f), Reflection.Diffuse),
                new Sphere(5f,    new Vector3(50,         75,            81.6f), new Vector3(0, 0, 0), new Vector3(0, .682f, .999f), Reflection.Diffuse),
              };

var camera = new Camera(140, 0.5135f, new Vector3(50, 52, 295.6f), new Vector3(0, -0.042612f, -1), Width, Height);

var payload = new TracerPayload(0, 0, Width, Height, Samples);
var scene   = new Scene(Width, Height, KillDepth, SplitDepth, camera, spheres);

var nbThreads = 2 * Process.GetCurrentProcess().Threads.Count;

Console.WriteLine("Start rendering on {0} threads", nbThreads);


var error  = float.PositiveInfinity;
var result = TracerCompute.ComputePayload(payload, scene, nbThreads);
while (error > 1)
{
  var sw = new Stopwatch();
  sw.Start();
  Console.WriteLine("Computing second");
  var secondResult = TracerCompute.ComputePayload(payload, scene, nbThreads, result);
  sw.Stop();
  Console.WriteLine("Elapsed: {0}", sw.Elapsed);

  error = new MSE().GetMeanMetric(result.RawSamples, secondResult.RawSamples);

  Console.WriteLine("Error : {0}", error);

  result = secondResult;

  var pixels = result.Pixels.Span;

  var file = new FileStream($"test-{error}.ppm", FileMode.OpenOrCreate, FileAccess.Write);

  file.Write(Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n"));

  for (var i = Height - 1; i >= 0; --i)
  {
    for (var j = 0; j < Width; ++j)
    {
      var offset = (i * Width + j) * 3;
      file.Write(pixels.Slice(offset + 2, 1));
      file.Write(pixels.Slice(offset + 1, 1));
      file.Write(pixels.Slice(offset + 0, 1));
    }
  }

  file.Flush();
  file.Close();
}




