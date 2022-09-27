namespace PiTracerWorker;

internal class SimpleBlas
{
  public static void copy(double[] x,
                          double[] y)
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

  public static void axpy(double   alpha,
                          double[] x,
                          double[] y)
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

  public static double dot(double[] a,
                           double[] b)
    => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

  public static double nrm2(double[] a)
    => Math.Sqrt(dot(a,
                     a));

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

  public static void normalize(double[] x)
    => scal(1 / nrm2(x),
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
}
