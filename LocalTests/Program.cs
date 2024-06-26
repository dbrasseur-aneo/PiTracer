using System.Numerics;
using System.Runtime.InteropServices;

using PiTracerLib;

const int Height       = 400;
const int Width        = 640;
const int Samples      = 200;
const int TotalSamples = 50;
const int KillDepth    = 7;
const int SplitDepth   = 0;
const int TaskHeight   = 32;
const int TaskWidth    = 32;

var spheres = new[]
              {
                new Sphere(1e5f,  new Vector3(1e5f  + 1,  40.8f,         81.6f), new Vector3(0, 0, 0), new Vector3(.75f, .25f, .25f), Reflection.Diffuse),
                new Sphere(1e5f,  new Vector3(-1e5f + 99, 40.8f,         81.6f), new Vector3(0, 0, 0), new Vector3(.25f, .25f, .75f), Reflection.Diffuse),
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

//var camera = new Camera(140, 0.5135f, new Vector3(50, 52, 295.6f), new Vector3(0, -0.042612f, -1));

//var payload = new TracerPayload(Width, Height, 0, 0, KillDepth, SplitDepth, Width, Height, Samples, camera, spheres);

//var payload2 = new TracerPayload(payload.ToBytes());

var values = new float[8];
var array  = new byte[8 * 4];
for (var i = 0; i < 8; i++)
{
  values[i] = MathF.PI * i;
  var arr = BitConverter.GetBytes(values[i]);
  for (int j = 0; j < 4; j++)
  {
    array[i * 4 + j] = arr[j];
  }

  
}
//Buffer.BlockCopy(values, 0, array, 0, 8*4);


Console.WriteLine(values);
Console.WriteLine(array);
Console.WriteLine(new Vector<float>(values));
Console.WriteLine(new Vector<float>(array));
