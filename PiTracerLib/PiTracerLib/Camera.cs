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

  public byte[] ToBytes()
  {
    var bytes = new byte[Size];
    BitConverter.GetBytes(Length).CopyTo(bytes, 0);
    BitConverter.GetBytes(CST).CopyTo(bytes, 4);
    BitConverterExt.GetBytes(Position).CopyTo(bytes, 8);
    BitConverterExt.GetBytes(Direction).CopyTo(bytes, 20);
    return bytes;
  }

  public Camera(float   length,
                float   cst,
                Vector3 position,
                Vector3 direction)
  {
    Length    = length;
    CST       = cst;
    Position  = position;
    Direction = Vector3.Normalize(direction);
  }

  public const int Size = 32;
}
