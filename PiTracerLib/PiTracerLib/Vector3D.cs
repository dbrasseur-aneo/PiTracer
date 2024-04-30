using System.Numerics;

namespace PiTracerLib;

public struct Vector3D
{
  public double X { get; set; }
  public double Y { get; set; }
  public double Z { get; set; }

  public Vector3D(double x,
                  double y,
                  double z)
  {
    X = x;
    Y = y;
    Z = z;
  }

  public Vector3D(Vector3 o)
  {
    X = o.X;
    Y = o.Y;
    Z = o.Z;
  }

  public double LengthSquared()
    => X * X + Y * Y + Z * Z;

  public double Length()
    => Math.Sqrt(LengthSquared());

  public static Vector3D Normalize(Vector3D x)
  {
    var qnorm = 1           / x.Length();
    return new Vector3D(x.X * qnorm, x.Y * qnorm, x.Z * qnorm);
  }

  public static double Dot(Vector3D x,
                           Vector3D y)
    => x.X * y.X + x.Y * y.Y + x.Z * y.Z;

  public static Vector3D Cross(Vector3D a,
                               Vector3D b)
    => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

  public static Vector3D operator +(Vector3D a,
                                    Vector3D b)
    => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

  public static Vector3D operator -(Vector3D a,
                                    Vector3D b)
    => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

  public static Vector3D operator *(Vector3D a,
                                    Vector3D b)
    => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

  public static Vector3D operator *(double   a,
                                    Vector3D b)
    => new(a * b.X, a * b.Y, a * b.Z);

  public static Vector3D Clamp(Vector3D a,
                               Vector3D min,
                               Vector3D max)
    => new(Math.Min(Math.Max(a.X, min.X), max.X), Math.Min(Math.Max(a.Y, min.Y), max.Y), Math.Min(Math.Max(a.Z, min.Z), max.Z));

  public static Vector3D operator -(Vector3D a)
    => new(-a.X, -a.Y, -a.Z);

  public static Vector3D Negate(Vector3D a)
    => -a;

  public static Vector3D Reflect(Vector3D a,
                                 Vector3D normal)
  {
    var an = 2 * Dot(a, normal);
    return new Vector3D(a.X - an * normal.X, a.Y - an * normal.Y * normal.Y, a.Z - an * normal.Z);
  }

  public Vector3D(Vector2 xy,
                  float   z)
  {
    X = xy.X;
    Y = xy.Y;
    Z = z;
  }

  public Vector3 AsVector3()
    => new((float)X, (float)Y, (float)Z);
}
