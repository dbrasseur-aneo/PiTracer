using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ArmoniK.Samples.PiTracer.Adapter
{

  public enum Reflection
  {
    Diff=0,
    Spec=1,
    Refr=2,
  }
  public class Sphere
  {
    [JsonPropertyName("radius")]
    public double Radius;

    [JsonPropertyName("position")]
    public double[] Position;

    [JsonPropertyName("emission")]
    public double[] Emission; /* couleur émise (=source de lumière) */

    [JsonPropertyName("color")]
    public double[] Color; /* couleur de l'objet RGB (diffusion, refraction, ...) */

    [JsonPropertyName("reflection")]
    public int Refl; /* type de reflection */

    [JsonPropertyName("max_reflectivity")]
    public double MaxReflexivity;
  }
}
