namespace PiTracerLib
{
  public struct TracerResult
  {
    public byte[] PayloadBytes { get; }
    public  int    CoordX     {
      get => BitConverter.ToInt32(PayloadBytes,
                                  0);
      set
        => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 0);
    }
    public int CoordY {
      get => BitConverter.ToInt32(PayloadBytes,
                                  4);
      set
        => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 4);
    }
    public int TaskWidth {
      get => BitConverter.ToInt32(PayloadBytes,
                                  8);
      set
        => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 8);
    }
    public int TaskHeight {
      get => BitConverter.ToInt32(PayloadBytes,
                                  12);
      set
        => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 12);
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

    public TracerResult(byte[] payload)
    {
      PayloadBytes = new byte[payload.Length];
      payload.CopyTo(PayloadBytes,0);
    }
  }
}
