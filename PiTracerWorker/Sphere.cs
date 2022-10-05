using System.Text.Json.Serialization;

namespace PiTracerWorker;

public enum Reflection
{
  Diff = 0,
  Spec = 1,
  Refr = 2,
}

public class Sphere
{
  [JsonPropertyName("radius")]
  public float Radius { get; set; }

  [JsonPropertyName("position")]
  public float[] Position { get; set; }

  [JsonPropertyName("emission")]
  public float[] Emission { get; set; } /* couleur émise (=source de lumière) */

  [JsonPropertyName("color")]
  public float[] Color { get; set; } /* couleur de l'objet RGB (diffusion, refraction, ...) */

  [JsonPropertyName("reflection")]
  public int Refl { get; set; } /* type de reflection */

  [JsonPropertyName("max_reflectivity")]
  public float MaxReflexivity { get; set; }

  public override string ToString()
    => $"Radius : {Radius}, Position : {"[" + string.Join(", ", Position.ToArray()) + "]"}, Emission : {"[" + string.Join(", ", Emission.ToArray()) + "]"}, Color : {"[" + string.Join(", ", Color.ToArray()) + "]"}, Reflection : {Refl}, MaxReflectivity {MaxReflexivity}";
}
