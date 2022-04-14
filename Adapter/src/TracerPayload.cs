using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ArmoniK.Samples.PiTracer.Adapter
{
  public class TracerPayload
  {
    [JsonPropertyName("img_width")]
    public int ImgWidth { get; set; }

    [JsonPropertyName("img_height")]
    public int ImgHeight { get; set; }

    [JsonPropertyName("coord_x")]
    public int CoordX { get; set; }

    [JsonPropertyName("coord_y")]
    public int CoordY { get; set; }

    [JsonPropertyName("kill_depth")]
    public int KillDepth { get; set; }

    [JsonPropertyName("split_depth")]
    public int SplitDepth { get; set; }

    [JsonPropertyName("spheres")]
    public IList<Sphere> Spheres { get; set; }

    [JsonPropertyName("task_width")]
    public int TaskWidth { get; set; }

    [JsonPropertyName("task_height")]
    public int TaskHeight { get; set; }

    public byte[] serialize()
    {
      var jsonString = JsonSerializer.Serialize(this);
      return Encoding.ASCII.GetBytes(stringToBase64(jsonString));
    }

    public static TracerPayload deserialize(byte[] payload, ILogger logger)
    {
      if (payload == null || payload.Length == 0)
        return new TracerPayload()
        {
          ImgWidth = 0,
          ImgHeight = 0,
          CoordX  = 0,
          CoordY = 0,
          KillDepth = 0,
          SplitDepth = 0,
          TaskWidth = 0,
          TaskHeight = 0,
          Spheres = new List<Sphere>(),
        };

      var str = Encoding.ASCII.GetString(payload);
      str = Base64ToString(str);
      logger.LogWarning(str);
      return JsonSerializer.Deserialize<TracerPayload>(str);
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
