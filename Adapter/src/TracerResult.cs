using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ArmoniK.Samples.PiTracer.Adapter
{
  public class TracerResult
  {
    [JsonPropertyName("coord_x")] 
    public int CoordX { get; set; }

    [JsonPropertyName("coord_y")] 
    public int CoordY { get; set; }

    [JsonPropertyName("task_width")] 
    public int TaskWidth { get; set; }

    [JsonPropertyName("task_height")] 
    public int TaskHeight { get; set; }

    [JsonPropertyName("pixels")]
    public byte[] Pixels { get; set; }

    public byte[] serialize()
    {
      var jsonString = JsonSerializer.Serialize(this);
      return Encoding.ASCII.GetBytes(stringToBase64(jsonString));
    }

    public static TracerResult deserialize(byte[] payload)
    {
      if (payload == null || payload.Length == 0)
        return new TracerResult()
        {
          CoordX     = 0,
          CoordY     = 0,
          TaskWidth  = 0,
          TaskHeight = 0,
          Pixels = new byte[]{},
        };

      var str = Encoding.ASCII.GetString(payload);
      return JsonSerializer.Deserialize<TracerResult>(Base64ToString(str));
    }

    private static string stringToBase64(string serializedJson)
    {
      var serializedJsonBytes       = Encoding.UTF8.GetBytes(serializedJson);
      var serializedJsonBytesBase64 = Convert.ToBase64String(serializedJsonBytes);
      return serializedJsonBytesBase64;
    }

    private static string Base64ToString(string base64)
    {
      var c = Convert.FromBase64String(base64);
      return Encoding.ASCII.GetString(c);
    }
  }
}
