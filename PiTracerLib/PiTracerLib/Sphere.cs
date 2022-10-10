using System.Numerics;

namespace PiTracerLib
{
  public enum Reflection{
    Diffuse    =0,
    Specular   =1,
    Refraction =2,
  }
  public readonly struct Sphere
  {
    public float      Radius         { get; }
    public Vector3D    Position       { get; }
    public Vector3D    Emission       { get; } /* couleur émise (=source de lumière) */
    public Vector3D    Color          { get; } /* couleur de l'objet RGB (diffusion, refraction, ...) */
    public Reflection Refl           { get; } /* type de reflection */
    public double      MaxReflexivity { get; }
    public Sphere(byte[] totalPayload, int spherePayloadOffset=0)
    {
      Radius = BitConverter.ToSingle(totalPayload, spherePayloadOffset);
      Position = new Vector3D(BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset+ 4),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 8),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 12));
      Emission = new Vector3D(BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 16),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 20),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 24));
      Color = new Vector3D(BitConverter.ToSingle(totalPayload,
                                                spherePayloadOffset + 28),
                          BitConverter.ToSingle(totalPayload,
                                                spherePayloadOffset + 32),
                          BitConverter.ToSingle(totalPayload,
                                                spherePayloadOffset + 36));
      Refl = (Reflection)BitConverter.ToInt32(totalPayload,
                                  spherePayloadOffset + 40);
      MaxReflexivity = Math.Max(Math.Max(Color.X,
                                         Color.Y),
                                Color.Z);
    }

    public Sphere(float      radius,
                  Vector3D    position,
                  Vector3D    emission,
                  Vector3D    color,
                  Reflection refl,
                  float      maxReflexivity)
    {
      Radius         = radius;
      Position       = position;
      Emission       = emission;
      Color          = color;
      Refl           = refl;
      MaxReflexivity = Math.Max(Math.Max(Color.X,
                                         Color.Y),
                                Color.Z);
    }

    public byte[] ToBytes()
    {
      var bytes = new byte[Size];
      BitConverter.GetBytes(Radius).CopyTo(bytes, 0);
      BitConverterExt.GetBytes(Position.AsVector3()).CopyTo(bytes, 4);
      BitConverterExt.GetBytes(Emission.AsVector3()).CopyTo(bytes, 16);
      BitConverterExt.GetBytes(Color.AsVector3()).CopyTo(bytes, 28);
      BitConverter.GetBytes((int)Refl).CopyTo(bytes, 40);
      BitConverter.GetBytes(MaxReflexivity).CopyTo(bytes, 44);
      return bytes;
    }

    public const int Size = 48;
  }
}
