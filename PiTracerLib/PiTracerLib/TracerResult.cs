namespace PiTracerLib
{
  public struct TracerResult
  {
    public byte[] PayloadBytes { get; }
    public  int    CoordX     {
      get => BitConverter.ToInt32(PayloadBytes,
                                  0);
      set
        => BitConverter.GetBytes(value).CopyTo(PayloadBytes.AsSpan(0,
                                                                   4) );
    }
    public int CoordY {
      get => BitConverter.ToInt32(PayloadBytes,
                                  4);
      set
        => BitConverter.TryWriteBytes(PayloadBytes.AsSpan(4,
                                                          4),
                                      value);
    }
    public int TaskWidth {
      get => BitConverter.ToInt32(PayloadBytes,
                                  8);
      set
        => BitConverter.TryWriteBytes(PayloadBytes.AsSpan(8,
                                                          4),
                                      value);
    }
    public int TaskHeight {
      get => BitConverter.ToInt32(PayloadBytes,
                                  12);
      set
        => BitConverter.TryWriteBytes(PayloadBytes.AsSpan(12,
                                                          4),
                                      value);
    }

    public Memory<byte> Pixels
    {
      get
        => new(PayloadBytes,
               16,
               PayloadBytes.Length - 16);
      set => value.CopyTo(Pixels);
    }

    public TracerResult(int nPixels)
    {
      PayloadBytes =
        new byte[4*4 + nPixels*3];
    }
  }
}
