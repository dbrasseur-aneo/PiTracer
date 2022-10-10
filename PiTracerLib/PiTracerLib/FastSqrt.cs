using System.Runtime.InteropServices;

namespace PiTracerLib;

public static class Fast
{

  [StructLayout(LayoutKind.Explicit,
                Pack = 1)]
  private struct UnionFloat
  {
    [FieldOffset(0)]
    public float d;

    [FieldOffset(0)]
    public uint l;
  }

  public static float QSqrt(float x)
  {
    var x2 = x * 0.5f;
    var u = new UnionFloat
            {
              d = x,
            };
    // The magic number is for doubles is from https://cs.uwaterloo.ca/~m32rober/rsqrt.pdf
    u.l =  0x5f3759df - (u.l >> 1);
    u.d *= (1.5f - (x2 * u.d * u.d)); // 1st iteration
    //      y  = y * ( 1.5 - ( x2 * y * y ) );   // 2nd iteration, this can be removed
    return u.d;
  }

  public static float Sqrt(float x)
  {
    var x2 = x * 0.5f;
    var u = new UnionFloat
            {
              d = x,
            };
    u.l =  0x5f3759df - (u.l >> 1);
    u.d *= (1.5f - (x2 * u.d * u.d)); // 1st iteration
    //      y  = y * ( 1.5 - ( x2 * y * y ) );   // 2nd iteration, this can be removed
    return u.d*x;
  }
}
