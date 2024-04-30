using System.Numerics;
using System.Runtime.CompilerServices;

namespace PiTracerLib;

public static class Fast
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QSqrt(float x)
  {
    var y  = MathF.ReciprocalSqrtEstimate(x);
    var x2 = x * 0.5f;
    y *= 1.5f - x2 * y * y;
    return y;
  }

  public static float Sqrt(float x)
    => QSqrt(x) * x;

  public static Vector3 Normalize(in Vector3 x)
    => x * QSqrt(x.LengthSquared());
}
