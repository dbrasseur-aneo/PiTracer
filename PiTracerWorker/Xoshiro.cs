using System.Runtime.InteropServices;

namespace PiTracerWorker;

public class Xoshiro
{
  public static double next_double(ulong[] s)
  {
    var s0     = s[0];
    var s1     = s[1];
    var result = s0 + s1;

    s1   ^= s0;
    s[0] =  ((s0 << 24) | (s0 >> (64 - 24))) ^ s1 ^ (s1 << 16);                     // a, b
    s[1] =  (s1                                         << 37) | (s1 >> (64 - 37)); // c

    var u = new Union
            {
              l = 0x3FF0000000000000 | (result >> 12),
            };
    return u.d - 1;
  }

  [StructLayout(LayoutKind.Explicit,
                Pack = 1)]
  private struct Union
  {
    [FieldOffset(0)]
    public readonly double d;

    [FieldOffset(0)]
    public ulong l;
  }
}
