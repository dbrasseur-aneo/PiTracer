using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PiTracerLib;

public readonly struct SphereList : IReadOnlyList<Sphere>
{
  public readonly int      Length;
  public readonly int      Capacity;
  public readonly int      Cardinal;
  public readonly float[]  Radius;
  public readonly float[]  PositionX;
  public readonly float[]  PositionY;
  public readonly float[]  PositionZ;
  public readonly Sphere[] Spheres;

  private readonly Vector<float> Zero;
  private readonly Vector<float> One;
  private readonly Vector<float> mOne;
  private readonly Vector<float> Epsilon;
  private readonly Vector<int>   Iota;
  private readonly Vector<int>   Incr;

  public Sphere this[int i]
    => Spheres[i];

  public SphereList(Sphere[] spheres)
  {
    Cardinal = Vector<float>.Count;
    Zero     = Vector<float>.Zero;
    One      = Vector<float>.One;
    mOne     = -One;
    Epsilon  = new Vector<float>(7e-2f);
    Incr     = new Vector<int>(Cardinal);
    var iota = new int[Cardinal];
    for (var i = 0; i < Cardinal; ++i)
    {
      iota[i] = i;
    }

    Iota = new Vector<int>(iota);


    Length    = spheres.Length;
    Capacity  = (Length + Cardinal - 1) & -Cardinal;
    Radius    = new float[Capacity];
    PositionX = new float[Capacity];
    PositionY = new float[Capacity];
    PositionZ = new float[Capacity];
    Spheres   = spheres;

    for (var i = 0; i < Length; ++i)
    {
      var sphere = spheres[i];
      Radius[i]    = sphere.Radius;
      PositionX[i] = sphere.Position.X;
      PositionY[i] = sphere.Position.Y;
      PositionZ[i] = sphere.Position.Z;
    }

    for (var i = Length; i < Capacity; ++i)
    {
      Radius[i]    = 0.0f;
      PositionX[i] = 0.0f;
      PositionY[i] = 0.0f;
      PositionZ[i] = 0.0f;
    }
  }

  public (int, float) Intersect(in Vector3 origin,
                                in Vector3 direction)
  {
    // Todo: https://developer.arm.com/documentation/102753/0100/Neon-Intrinsics-examples
    var oX = new Vector<float>(origin.X);
    var oY = new Vector<float>(origin.Y);
    var oZ = new Vector<float>(origin.Z);

    var dX = new Vector<float>(direction.X);
    var dY = new Vector<float>(direction.Y);
    var dZ = new Vector<float>(direction.Z);

    var found   = new Vector<int>(-1);
    var closest = new Vector<float>(float.PositiveInfinity);
    var idx     = Iota;


    for (var i = 0; i < Capacity; i += Cardinal, idx += Incr)
    {
      var r  = new Vector<float>(Radius,    i);
      var pX = new Vector<float>(PositionX, i);
      var pY = new Vector<float>(PositionY, i);
      var pZ = new Vector<float>(PositionZ, i);

      var fX = oX - pX;
      var fY = oY - pY;
      var fZ = oZ - pZ;

      var b  = -(fX * dX + fY * dY + fZ * dZ);
      var r2 = r * r;

      var zX = fX + b * dX;
      var zY = fY + b * dY;
      var zZ = fZ + b * dZ;

      var delta = r2 - (zX * zX + zY * zY + zZ * zZ);
      var bSign = Vector.ConditionalSelect(Vector.LessThan(b, Zero), mOne, One);
      var q     = b + bSign * Vector.SquareRoot(delta);

      var t0 = (fX * fX + fY * fY + fZ * fZ - r2) / q;
      var t1 = q;

      var chooseT0 = Vector.GreaterThan(t0, Epsilon);

      var t = Vector.ConditionalSelect(chooseT0, t0, t1);

      var candidate = Vector.GreaterThan(delta, Zero);
      candidate &= Vector.GreaterThan(t, Epsilon);
      candidate &= Vector.LessThan(t, closest);

      found   = Vector.ConditionalSelect(candidate, idx, found);
      closest = Vector.ConditionalSelect(candidate, t,   closest);
    }

    var res     = -1;
    var resDist = float.PositiveInfinity;
    for (var i = 0; i < Cardinal; ++i)
    {
      var d = closest[i];
      if (d < resDist)
      {
        res     = found[i];
        resDist = d;
      }
    }

    return (res, resDist);
  }

  public IEnumerator<Sphere> GetEnumerator()
    => (Spheres as IEnumerable<Sphere>).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator()
    => Spheres.GetEnumerator();

  public int Count
    => Length;
}
