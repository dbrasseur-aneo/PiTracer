using System.Collections.Concurrent;
using System.Numerics;

namespace PiTracerLib;

public static class TracerCompute
{
  private const float AirRefractionIndex        = 1.0f;
  private const float GlassRefractionIndex      = 1.5f;
  private const float Gamma                     = 1                    / 2.2f;
  private const float AirToGlassRefractionIndex = AirRefractionIndex   / GlassRefractionIndex;
  private const float GlassToAirRefractionIndex = GlassRefractionIndex / AirRefractionIndex;
  private const float BaseReflectance = ((GlassRefractionIndex - AirRefractionIndex) * (GlassRefractionIndex - AirRefractionIndex)) /
                                        ((GlassRefractionIndex + AirRefractionIndex) * (GlassRefractionIndex + AirRefractionIndex));
  private const float Infinity = float.PositiveInfinity;
  private const float Epsilon  = 1e-4f;
  private static Vector3D GenRandomUnitVector(ulong[] s, in Vector3D normal)
  {
    Vector3D v;

    do
    {
      v = new Vector3D(Xoshiro.next_double(s), Xoshiro.next_double(s),Xoshiro.next_double(s));
    } while (v.LengthSquared() is > 1 or < 0.001f && Vector3D.Dot(normal, v) < 0);

    return Vector3D.Normalize(v);

  }

  private static byte ToInt(double x)
  {
    var value = (Math.Pow(x, Gamma) * 255 + 0.5);

    return (byte)((int)value); /* gamma correction = 2.2 */
  }


  public static Vector3D Radiance(in TracerPayload payload,
                                 in Vector3D       origin,
                                 in Vector3D       direction,
                                 int              depth,
                                 ulong[]          state)
  {
    var distance = Intersect(payload.Spheres, origin, direction, out var id);
    if ( distance >= Infinity)
    {
      //No sphere were intersected by the ray, return black
      return new Vector3D(Vector3.Zero);
    }

    //Intersected sphere
    var obj = payload.Spheres[id];

    var objColor = new Vector3D(obj.Color.X, obj.Color.Y, obj.Color.Z);

    // After a certain depth, random kill of the ray
    // depending on the object reflectivity,
    // otherwise fade the ray's acquired object color
    depth++;
    if (depth > payload.KillDepth)
    {
      if (Xoshiro.next_double(state) < obj.MaxReflexivity && depth < payload.KillDepth)
      {
        objColor = ( 1 / obj.MaxReflexivity) * objColor;
      }
      else
      {
        return obj.Emission;
      }
    }

    var intersectionPoint = distance * direction + origin;
    var normal            = Vector3D.Normalize(intersectionPoint - obj.Position);
    var into = Vector3D.Dot(normal,
                           direction) < 0;
    var normalOppositeToRay = into
                                ? new Vector3D(normal.X, normal.Y, normal.Z)
                                : Vector3D.Negate(normal);

    if (obj.Refl == Reflection.Diffuse)
    {
      //Diffuse reflection : send random ray and get its radiance
      var randomRay = GenRandomUnitVector(state, normalOppositeToRay);

      //Ponder the received ray by the object color and add the emission of the object
      return obj.Emission + objColor * Radiance(payload,
                                                intersectionPoint,
                                                randomRay,
                                                depth,
                                                state);
    }
    
    //Compute the reflected ray
    var reflected = Vector3D.Reflect(direction,normal);
    if (obj.Refl == Reflection.Specular)
    {
      //Perfect mirror
      return obj.Emission + objColor * Radiance(payload,
                                                intersectionPoint,
                                                reflected,
                                                depth,
                                                state);
    }

    //Dielectric surface (glass)

    // Is ray from the exterior of the sphere ?
    var surfaceRefractionFactor = into
                              ? AirToGlassRefractionIndex
                              : GlassToAirRefractionIndex;
    var angleOfAttack = Vector3D.Dot(direction,
                                    normalOppositeToRay);

    //If the ray's angle of attack is too shallow, total reflection
    var cos2T = 1 - surfaceRefractionFactor * surfaceRefractionFactor * (1 - angleOfAttack * angleOfAttack);
    if (cos2T < 0)
    {
      return obj.Emission + objColor * Radiance(payload,
                                                             intersectionPoint,
                                                             reflected,
                                                             depth,
                                                             state);
    }

    //Refracted direction computation
    var refracted = surfaceRefractionFactor * direction - (into ? 1 : -1) * (angleOfAttack * surfaceRefractionFactor + Math.Sqrt(cos2T)) * normal;

    //Compute reflectance
    var reflectanceFactor = 1 - (into
                                   ? -angleOfAttack
                                   : Vector3D.Dot(refracted,
                                                 normal));
    var reflectance   = BaseReflectance + (1 - BaseReflectance) * reflectanceFactor * reflectanceFactor * reflectanceFactor * reflectanceFactor * reflectanceFactor;
    var transmittance = 1               - reflectance;

    //Above a given depth threshold, we compute either the refracted or the reflected ray.
    //Below the threshold, we compute both
    Vector3D received;
    if (depth > payload.SplitDepth)
    {
      var reflectionProbability = 0.25 + 0.5 * reflectance;
      if (Xoshiro.next_double(state) < reflectionProbability)
      {
        received = (reflectance / reflectionProbability) * Radiance(payload,
                                                                    intersectionPoint,
                                                                    reflected,
                                                                    depth,
                                                                    state);
      }
      else
      {
        received = (transmittance / (1 - reflectionProbability)) * Radiance(payload,
                                                                            intersectionPoint,
                                                                            refracted,
                                                                            depth,
                                                                            state);
      }
    }
    else
    {
      received = reflectance * Radiance(payload,
                                        intersectionPoint,
                                        reflected,
                                        depth,
                                        state) + transmittance * Radiance(payload,
                                                                          intersectionPoint,
                                                                          refracted,
                                                                          depth,
                                                                          state);
    }

    return obj.Emission + objColor * received;
    

  }

  public static float Intersect(in  ReadOnlySpan<Sphere> spheres,
                                in  Vector3D              origin,
                                in  Vector3D              direction,
                                out int                  id)
  {
    var distance = Infinity;
    id = -1;
    for (var i = 0; i < spheres.Length; i++)
    {
      var current = SphereIntersect(spheres[i],
                                    origin,
                                    direction);
      if (current > 0 && current < distance)
      {
        distance = current;
        id = i;
      }
    }

    return distance;
  }


  public static float SphereIntersect(in Sphere  sphere,
                                      in Vector3D origin,
                                      in Vector3D direction)
  {
    //https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection
    var originToCenter = sphere.Position - origin;
    var b = Vector3D.Dot(originToCenter,
                        direction);
    var discriminant = b * b - Vector3D.Dot(originToCenter, originToCenter) + sphere.Radius * sphere.Radius;
    if (discriminant < 0)
    {
      //No intersection
      return 0;
    }

    discriminant = Math.Sqrt(discriminant);
    var closest = b - discriminant;
    if (closest > Epsilon)
    {
      return (float)closest;
    }
    closest = b + discriminant;
    if (closest > Epsilon)
    {
      return (float)closest;
    }
    // Too shallow, strange cases...
    return 0;
  }

  public static TracerResult ComputePayload(TracerPayload payload,
                                            int           nThreads)
  {
    // X increment to go from one pixel to the next
    var incX = new Vector3D(payload.ImgWidth * payload.Camera.CST / payload.ImgHeight,
                               0,
                               0);
    var incY = payload.Camera.CST * Vector3D.Normalize(Vector3D.Cross(incX,
                                                                    payload.Camera.Direction));
    var options = new ParallelOptions
                  {
                    MaxDegreeOfParallelism = nThreads,
                  };
    var states       = new ConcurrentDictionary<int, ulong[]>();
    var sampleWeight = 1.0f / payload.Samples;

    var result = new TracerResult(payload.TaskHeight * payload.TaskWidth)
                 {
                   TaskHeight = payload.TaskHeight,
                   TaskWidth = payload.TaskWidth,
                   CoordX = payload.CoordX,
                   CoordY = payload.CoordY,
                 };
    var image  = new byte[payload.TaskHeight * payload.TaskWidth * 3];
    Parallel.For(0,
                 payload.TaskHeight * payload.TaskWidth,
                 options,
                 offset =>
                 {

                   var state = states.GetOrAdd(Environment.CurrentManagedThreadId,
                                               _ =>
                                               {
                                                 var rand = new Random();
                                                 return new[]
                                                        {
                                                          (ulong)rand.NextInt64(),
                                                          (ulong)rand.NextInt64(),
                                                        };
                                               });

                   var i             = payload.CoordX + offset / payload.TaskWidth;
                   var j             = payload.CoordY + offset % payload.TaskWidth;
                   var pixelRadiance = new Vector3D(0,0,0);

                   for (var s = 0; s < payload.Samples; s++)
                   {
                     var randX = 2 * Xoshiro.next_double(state);
                     var randY = 2 * Xoshiro.next_double(state);
                     var dx    = (randX < 1) ? Math.Sqrt(randX) - 1 : 1 - Math.Sqrt(2 - randX);
                     var dy    = (randY < 1) ? Math.Sqrt(randY) - 1 : 1 - Math.Sqrt(2 - randY);
                     var rayDirection = Vector3D.Normalize(payload.Camera.Direction +
                                                          (((1 + dy) * 0.5f + i) / payload.ImgHeight - 0.5f) * incY +
                                                          (((1 + dx) * 0.5f + j) / payload.ImgWidth  - 0.5f) * incX);
                     var rayOrigin = payload.Camera.Position + payload.Camera.Length * rayDirection;
                     pixelRadiance += sampleWeight * Radiance(payload, rayOrigin, rayDirection, 0, state);
                   }

                   pixelRadiance = Vector3D.Clamp(pixelRadiance, new Vector3D(Vector3.Zero), new Vector3D(Vector3.One));
                   var index = offset * 3;
                   //BGR instead of RGB
                   image[index]     = ToInt(pixelRadiance.Z);
                   image[index + 1] = ToInt(pixelRadiance.Y);
                   image[index + 2] = ToInt(pixelRadiance.X);
                 });
    image.CopyTo(result.Pixels);
    return result;
  }

}
