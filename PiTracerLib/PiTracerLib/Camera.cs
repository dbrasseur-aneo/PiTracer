using System.Numerics;

namespace PiTracerLib;

public readonly struct Camera
{
  public float   Length    { get; }
  // ReSharper disable once InconsistentNaming
  public float   CST       { get; }
  public Vector3 Position  { get; }
  public Vector3 Direction { get; }

  public Vector3 IncX { get; }

  public Vector3 IncY { get; }

  public Camera(byte[] payload,
                int imgWidth,
                int imgHeight,
                int    payloadOffset = 0)
  {
    Length = BitConverter.ToSingle(payload, payloadOffset);
    CST    = BitConverter.ToSingle(payload, payloadOffset + 4);
    Position = new Vector3(BitConverter.ToSingle(payload, payloadOffset + 8), BitConverter.ToSingle(payload, payloadOffset + 12),
                           BitConverter.ToSingle(payload, payloadOffset + 16));
    Direction = Vector3.Normalize(new Vector3(BitConverter.ToSingle(payload, payloadOffset + 20), BitConverter.ToSingle(payload, payloadOffset + 24),
                                              BitConverter.ToSingle(payload, payloadOffset + 28)));
    IncX = new Vector3(imgWidth * CST / imgHeight, 0, 0);
    IncY = CST * Fast.Normalize(Vector3.Cross(IncX, Direction));
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
                Vector3 direction,
                int imgWidth,
                int     imgHeight)
  {
    Length    = length;
    CST       = cst;
    Position  = position;
    Direction = Vector3.Normalize(direction);
    IncX      = new Vector3(imgWidth * CST / imgHeight, 0, 0);
    IncY      = CST * Fast.Normalize(Vector3.Cross(IncX, Direction));
  }

  public const int Size = 32;
}
