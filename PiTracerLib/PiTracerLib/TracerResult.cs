using System.Numerics;
using System.Text;

namespace PiTracerLib;

public struct TracerResult
{
  public byte[] PayloadBytes { get; }

  public int CoordX
  {
    get => BitConverter.ToInt32(PayloadBytes, 0);
    set => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 0);
  }

  public int CoordY
  {
    get => BitConverter.ToInt32(PayloadBytes, 4);
    set => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 4);
  }

  public int TaskWidth
  {
    get => BitConverter.ToInt32(PayloadBytes, 8);
    set => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 8);
  }

  public int TaskHeight
  {
    get => BitConverter.ToInt32(PayloadBytes, 12);
    set => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 12);
  }

  public int NSamplesPerPixel
  {
    get => BitConverter.ToInt32(PayloadBytes, 16);
    set => BitConverter.GetBytes(value).CopyTo(PayloadBytes, 16);
  }

  public bool IsFinal
  {
    get => BitConverter.ToInt32(PayloadBytes, 20) != 0;
    set => BitConverter.GetBytes(value ? 1 : 0).CopyTo(PayloadBytes, 20);
  }

  private const int PixelsOffset = 24;

  private int NPixels
    => TaskWidth * TaskHeight;

  private int PixelFieldSize
    => NPixels * 3;

  private int SamplesOffset
    => PixelsOffset + PixelFieldSize;

  private const int SampleSize = sizeof(float) * 3;

  private int SamplesFieldSize
    => NPixels * SampleSize;

  private int NextResultIdOffset
    => SamplesOffset + SamplesFieldSize;

  private const int NextResultIdSize = 36;

  public Memory<byte> Pixels
  {
    get => new(PayloadBytes, PixelsOffset, PixelFieldSize);
    set => value.CopyTo(Pixels);
  }

  public ICollection<Vector3> Samples
  {
    get
    {
      var value = new List<Vector3>(NPixels);
      for (var i = 0; i < NPixels; i++)
      {
        value.Add(BitConverterExt.ToVector3(PayloadBytes, SamplesOffset + i * SampleSize));
      }

      return value;
    }
    set
    {
      foreach (var (i, v) in value.Select((v,
                                           i) => (i, v)))
      {
        BitConverterExt.GetBytes(v).CopyTo(PayloadBytes, SamplesOffset + i * SampleSize);
      }
    }
  }

  public Span<byte> RawSamples => PayloadBytes.AsSpan(SamplesOffset, SamplesFieldSize);

  public Vector3 GetSamples(int index)
    => BitConverterExt.ToVector3(PayloadBytes, SamplesOffset + index * SampleSize);

  public void SetSamples(int     index,
                         Vector3 value)
    => BitConverterExt.GetBytes(value).CopyTo(PayloadBytes, SamplesOffset + index * SampleSize);

  public string NextResultId
  {
    get => Encoding.ASCII.GetString(PayloadBytes, NextResultIdOffset, NextResultIdSize);
    set => Encoding.ASCII.GetBytes(value)[..NextResultIdSize].CopyTo(PayloadBytes, NextResultIdOffset);
  }
  public TracerResult(int nPixels)
  {
    PayloadBytes = new byte[PixelsOffset + nPixels * 3 + nPixels * SampleSize + NextResultIdSize];
    TaskWidth    = nPixels;
    TaskHeight   = 1;
  }

  public TracerResult(byte[] payload)
  {
    PayloadBytes = new byte[payload.Length];
    payload.CopyTo(PayloadBytes, 0);
  }
}
