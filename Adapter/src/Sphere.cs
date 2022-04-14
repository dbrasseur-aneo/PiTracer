using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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
    public double Radius { get; set; }

    [JsonPropertyName("position")]
    public double[] Position { get; set; }

    [JsonPropertyName("emission")]
    public double[] Emission { get; set; } /* couleur émise (=source de lumière) */

    [JsonPropertyName("color")]
    public double[] Color { get; set; } /* couleur de l'objet RGB (diffusion, refraction, ...) */

    [JsonPropertyName("reflection")]
    public int Refl{ get; set; } /* type de reflection */

    [JsonPropertyName("max_reflectivity")]
    public double MaxReflexivity { get; set; }

    public override string ToString()
    {
      return
        $"Radius : {Radius}, Position : {"["+string.Join(", ", Position.ToArray())+"]"}, Emission : {"["+string.Join(", ", Emission.ToArray())+"]"}, Color : {"["+string.Join(", ", Color.ToArray())+"]"}, Reflection : {Refl}, MaxReflectivity {MaxReflexivity}";
    }
  }
}
