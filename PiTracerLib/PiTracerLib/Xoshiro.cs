using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PiTracerLib;

public class Xoshiro
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ulong next_ulong(ulong[] s)
  {
    var s0     = s[0];
    var s1     = s[1];
    var result = s0 + s1;

    s1   ^= s0;
    s[0] =  ((s0 << 24) | (s0 >> (64 - 24))) ^ s1 ^ (s1 << 16);                     // a, b
    s[1] =  (s1                                         << 37) | (s1 >> (64 - 37)); // c
    return result;
  }

  public static double next_double(ulong[] s)
  {
    var res = next_ulong(s);
    var u = new Union
            {
              l = 0x3FF0000000000000 | (res >> 12),
            };
    return u.d - 1;
  }

  public static Vector2 next_float2(ulong[] s)
  {
    var res = next_ulong(s);
    var u = new FloatUnion
            {
              l = 0x3F8000003F800000 | ((res >> 9) & 0x3FFFFFFF3FFFFFFF),
            };
    return new Vector2(u.f1 - 1, u.f2 - 1);
  }

  public static float next_float(ulong[] s)
    => next_float2(s).X;

  [StructLayout(LayoutKind.Explicit, Pack = 1)]
  private struct Union
  {
    [FieldOffset(0)]
    public readonly double d;

    [FieldOffset(0)]
    public ulong l;
  }

  [StructLayout(LayoutKind.Explicit, Pack = 1)]
  private struct FloatUnion
  {
    [FieldOffset(0)]
    public ulong l;

    [FieldOffset(0)]
    public readonly float f1;

    [FieldOffset(4)]
    public readonly float f2;
  }
}
