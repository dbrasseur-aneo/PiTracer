namespace PiTracerLib;

public readonly struct TracerPayload
{
  public int        CoordX     { get; }
  public int        CoordY     { get; }
  public int        TaskWidth  { get; }
  public int        TaskHeight { get; }
  public int        Samples    { get; }

  public TracerPayload(byte[] payload)
  {
    CoordX     = BitConverter.ToInt32(payload, 0);
    CoordY     = BitConverter.ToInt32(payload, 4);
    TaskWidth  = BitConverter.ToInt32(payload, 8);
    TaskHeight = BitConverter.ToInt32(payload, 12);
    Samples    = BitConverter.ToInt32(payload, 16);
  }

  public TracerPayload(int      coordX,
                       int      coordY,
                       int      taskWidth,
                       int      taskHeight,
                       int      samples)
  {
    CoordX     = coordX;
    CoordY     = coordY;
    TaskWidth  = taskWidth;
    TaskHeight = taskHeight;
    Samples    = samples;
  }

  public byte[] ToBytes()
  {
    var bytes = new byte[20];
    BitConverter.GetBytes(CoordX).CopyTo(bytes, 0);
    BitConverter.GetBytes(CoordY).CopyTo(bytes, 4);
    BitConverter.GetBytes(TaskWidth).CopyTo(bytes, 8);
    BitConverter.GetBytes(TaskHeight).CopyTo(bytes, 12);
    BitConverter.GetBytes(Samples).CopyTo(bytes, 16);
    return bytes;
  }
}
