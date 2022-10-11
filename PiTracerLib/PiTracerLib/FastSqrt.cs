using System.Numerics;
using System.Runtime.CompilerServices;
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QSqrt(float x)
  {
    var y  = MathF.ReciprocalSqrtEstimate(x);
    var x2 = x * 0.5f;
    y *= (1.5f       - (x2 * y * y));
    return y;
  }

  public static float Sqrt(float x)
    => QSqrt(x) *x;

  public static Vector3 Normalize(in Vector3 x)
  {
    return x * QSqrt(x.LengthSquared());
  }
}
