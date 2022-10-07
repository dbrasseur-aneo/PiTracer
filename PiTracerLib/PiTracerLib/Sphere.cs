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
    public Vector3    Position       { get; }
    public Vector3    Emission       { get; } /* couleur émise (=source de lumière) */
    public Vector3    Color          { get; } /* couleur de l'objet RGB (diffusion, refraction, ...) */
    public Reflection Refl           { get; } /* type de reflection */
    public float      MaxReflexivity { get; }
    public Sphere(byte[] totalPayload, int spherePayloadOffset=0)
    {
      Radius = BitConverter.ToSingle(totalPayload, spherePayloadOffset);
      Position = new Vector3(BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset+ 4),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 8),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 12));
      Emission = new Vector3(BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 16),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 20),
                             BitConverter.ToSingle(totalPayload,
                                                   spherePayloadOffset + 24));
      Color = new Vector3(BitConverter.ToSingle(totalPayload,
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

    public const int Size = 48;
  }
}
