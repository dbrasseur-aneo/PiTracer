using System.Drawing;
using System.Numerics;

namespace PiTracerLib;

public readonly struct Camera
{
  public float   Length    { get; }
  public float   CST       { get; }
  public Vector3 Position  { get; }
  public Vector3 Direction { get; }

  public Camera(byte[] payload,
                int    payloadOffset = 0)
  {
    Length = BitConverter.ToSingle(payload,
                                   payloadOffset);
    CST = BitConverter.ToSingle(payload,
                                payloadOffset+4);
    Position = new Vector3(BitConverter.ToSingle(payload,
                                                 payloadOffset + 8),
                           BitConverter.ToSingle(payload,
                                                 payloadOffset + 12),
                           BitConverter.ToSingle(payload,
                                                 payloadOffset + 16));
    Direction = Vector3.Normalize(new Vector3(BitConverter.ToSingle(payload,
                                                                    payloadOffset + 20),
                                              BitConverter.ToSingle(payload,
                                                                    payloadOffset + 24),
                                              BitConverter.ToSingle(payload,
                                                                    payloadOffset + 28)));
  }

  public static int Size()
  {
    return 32;
  }
}
