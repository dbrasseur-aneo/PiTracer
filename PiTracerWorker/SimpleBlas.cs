using System.Runtime.InteropServices;

namespace PiTracerWorker;

public class SimpleBlas
{
  public static void copy(double[] x,
                          double[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] = x[i];
    }
  }

  public static void copy(float[] x,
                          float[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] = x[i];
    }
  }

  public static void zero(double[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] = 0;
    }
  }

  public static void zero(float[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] = 0;
    }
  }

  public static void axpy(double   alpha,
                          double[] x,
                          double[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] += alpha * x[i];
    }
  }

  public static void axpy(float   alpha,
                          float[] x,
                          float[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] += alpha * x[i];
    }
  }

  public static void scal(double   alpha,
                          double[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] *= alpha;
    }
  }

  public static void scal(float   alpha,
                          float[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] *= alpha;
    }
  }

  public static double dot(double[] a,
                           double[] b)
    => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

  public static float dot(float[] a,
                          float[] b)
    => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

  /********* fonction non-standard *************/
  public static void mul(double[] x,
                         double[] y,
                         double[] z)
  {
    for (var i = 0; i < 3; i++)
    {
      z[i] = x[i] * y[i];
    }
  }

  public static void mul(float[] x,
                         float[] y,
                         float[] z)
  {
    for (var i = 0; i < 3; i++)
    {
      z[i] = x[i] * y[i];
    }
  }

  public static void normalize(double[] x)
    => scal(fast_isqrt(dot(x,
                          x)),
                      x);

  public static void normalize(float[] x)
    => scal(fast_isqrt(dot(x,
                           x)),
            x);

  /* produit vectoriel */
  public static void cross(double[] a,
                           double[] b,
                           double[] c)
  {
    c[0] = a[1] * b[2] - a[2] * b[1];
    c[1] = a[2] * b[0] - a[0] * b[2];
    c[2] = a[0] * b[1] - a[1] * b[0];
  }

  public static void cross(float[] a,
                           float[] b,
                           float[] c)
  {
    c[0] = a[1] * b[2] - a[2] * b[1];
    c[1] = a[2] * b[0] - a[0] * b[2];
    c[2] = a[0] * b[1] - a[1] * b[0];
  }

/****** tronque *************/
  public static void clamp(double[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      if (x[i] < 0)
      {
        x[i] = 0;
      }

      if (x[i] > 1)
      {
        x[i] = 1;
      }
    }
  }

  public static void clamp(float[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      if (x[i] < 0)
      {
        x[i] = 0;
      }

      if (x[i] > 1)
      {
        x[i] = 1;
      }
    }
  }

  [StructLayout(LayoutKind.Explicit,
                Pack = 1)]
  private struct Union
  {
    [FieldOffset(0)]
    public double d;

    [FieldOffset(0)]
    public ulong l;
  }

  [StructLayout(LayoutKind.Explicit,
                Pack = 1)]
  private struct UnionFloat
  {
    [FieldOffset(0)]
    public float d;

    [FieldOffset(0)]
    public uint l;
  }

  public static double fast_sqrt(double x)
    => x * fast_isqrt(x);

  public static float fast_sqrt(float x)
    => x * fast_isqrt(x);

  public static double fast_isqrt(double x)
  {
    var x2 = x * 0.5;
    var  u = new Union
             {
               d = x,
             };
    // The magic number is for doubles is from https://cs.uwaterloo.ca/~m32rober/rsqrt.pdf
    u.l =  0x5fe6eb50c7b537a9 - (u.l >> 1);
    u.d *= (1.5 - (x2 * u.d * u.d)); // 1st iteration
    //      y  = y * ( 1.5 - ( x2 * y * y ) );   // 2nd iteration, this can be removed
    return u.d;
  }

  public static float fast_isqrt(float x)
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
}
