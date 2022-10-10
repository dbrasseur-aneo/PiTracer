using System.Numerics;

namespace PiTracerLib;

public static class BitConverterExt
{
  public static byte[] GetBytes(Vector3 vec)
  {
    var bytes = new byte[3 * 4];
    BitConverter.GetBytes(vec.X).CopyTo(bytes, 0);
    BitConverter.GetBytes(vec.Y).CopyTo(bytes, 4);
    BitConverter.GetBytes(vec.Z).CopyTo(bytes, 8);
    return bytes;
  }

  public static Vector3 ToVector3(byte[] value,
                                  int    startingIndex)
  {
    return new Vector3(BitConverter.ToSingle(value, startingIndex), BitConverter.ToSingle(value, startingIndex + 4), BitConverter.ToSingle(value, startingIndex + 8));
  }

}
