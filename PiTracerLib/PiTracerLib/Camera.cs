using System.Numerics;

namespace PiTracerLib;

public readonly struct Camera
{
  public float   Length    { get; }
  public float   CST       { get; }
  public Vector3D Position  { get; }
  public Vector3D Direction { get; }

  public Camera(byte[] payload,
                int    payloadOffset = 0)
  {
    Length = BitConverter.ToSingle(payload,
                                   payloadOffset);
    CST = BitConverter.ToSingle(payload,
                                payloadOffset+4);
    Position = new Vector3D(BitConverter.ToSingle(payload,
                                                 payloadOffset + 8),
                           BitConverter.ToSingle(payload,
                                                 payloadOffset + 12),
                           BitConverter.ToSingle(payload,
                                                 payloadOffset + 16));
    Direction = Vector3D.Normalize(new Vector3D(BitConverter.ToSingle(payload,
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
    BitConverterExt.GetBytes(Position.AsVector3()).CopyTo(bytes, 8);
    BitConverterExt.GetBytes(Direction.AsVector3()).CopyTo(bytes, 20);
    return bytes;
  }

  public Camera(float   length,
                float   cst,
                Vector3D position,
                Vector3D direction)
  {
    Length    = length;
    CST       = cst;
    Position  = position;
    Direction = Vector3D.Normalize(direction);
  }

  public const int Size = 32;
}
