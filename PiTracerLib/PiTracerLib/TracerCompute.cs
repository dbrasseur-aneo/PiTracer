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
  private const float Epsilon  = 7e-2f;
  private static Vector3 GenRandomUnitVector(ulong[] s, in Vector3 normal)
  {
    Vector3 v;

    do
    {
      v = new Vector3(Xoshiro.next_float2(s),Xoshiro.next_float(s));
    } while (v.LengthSquared() is > 1 or < 0.001f && Vector3.Dot(normal, v) < 0);

    return Fast.Normalize(v);

  }

  private static byte ToInt(float x)
  {
    var value = (MathF.Pow(x, Gamma) * 255 + 0.5f);

    return (byte)((int)value); /* gamma correction = 2.2 */
  }


  public static Vector3 Radiance(in TracerPayload payload,
                                 in Vector3       origin,
                                 in Vector3       direction,
                                 int              depth,
                                 ulong[]          state)
  {
    var distance = Intersect(payload.Spheres, origin, direction, out var id);
    if ( distance >= Infinity)
    {
      //No sphere were intersected by the ray, return black
      return Vector3.Zero;
    }

    //Intersected sphere
    var obj = payload.Spheres[id];

    // After a certain depth, random kill of the ray
    // depending on the object reflectivity,
    // otherwise fade the ray's acquired object color
    depth++;
    if (depth > payload.KillDepth)
    {
      return obj.Emission;
    }

    var intersectionPoint = distance * direction + origin;
    var normal            = Fast.Normalize(intersectionPoint - obj.Position);
    var into = Vector3.Dot(normal,
                           direction) < 0;
    var normalOppositeToRay = into
                                ? normal
                                : Vector3.Negate(normal);

    if (obj.Refl == Reflection.Diffuse)
    {
      //Diffuse reflection : send random ray and get its radiance
      var randomRay = GenRandomUnitVector(state, normalOppositeToRay);

      //Ponder the received ray by the object color and add the emission of the object
      return obj.Emission + obj.Color * Radiance(payload,
                                                intersectionPoint,
                                                randomRay,
                                                depth,
                                                state);
    }
    
    //Compute the reflected ray
    var reflected = Vector3.Reflect(direction,normal);
    if (obj.Refl == Reflection.Specular)
    {
      //Perfect mirror
      return obj.Emission + obj.Color * Radiance(payload,
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
    var angleOfAttack = Vector3.Dot(direction,
                                    normalOppositeToRay);

    //If the ray's angle of attack is too shallow, total reflection
    var cos2T = 1 - surfaceRefractionFactor * surfaceRefractionFactor * (1 - angleOfAttack * angleOfAttack);
    if (cos2T < 0)
    {
      return obj.Emission + obj.Color * Radiance(payload,
                                                             intersectionPoint,
                                                             reflected,
                                                             depth,
                                                             state);
    }

    //Refracted direction computation
    var refracted = surfaceRefractionFactor * direction - (into ? 1 : -1) * (angleOfAttack * surfaceRefractionFactor + (float)Math.Sqrt(cos2T)) * normal;

    //Compute reflectance
    var reflectanceFactor = 1 - (into
                                   ? -angleOfAttack
                                   : Vector3.Dot(refracted,
                                                 normal));
    var reflectance   = BaseReflectance + (1 - BaseReflectance) * reflectanceFactor * reflectanceFactor * reflectanceFactor * reflectanceFactor * reflectanceFactor;
    var transmittance = 1               - reflectance;

    //Above a given depth threshold, we compute either the refracted or the reflected ray.
    //Below the threshold, we compute both
    Vector3 received;
    if (depth > payload.SplitDepth)
    {
      var reflectionProbability = 0.25f + 0.5f * reflectance;
      if (Xoshiro.next_float(state) < reflectionProbability)
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

    return obj.Emission + obj.Color * received;
    

  }

  public static float Intersect(in  Sphere[] spheres,
                                in  Vector3              origin,
                                in  Vector3              direction,
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
                                      in Vector3 origin,
                                      in Vector3 direction)
  {
    var f     = origin - sphere.Position;
    var b     = Vector3.Dot(-f, direction);
    var z     = f + b * direction;
    var r2    = sphere.Radius * sphere.Radius;
    var delta = r2 - z.LengthSquared();
    

    if (delta < 0)
    {
      //No intersection
      return 0;
    }
    var q       = b + Math.Sign(b) * Fast.Sqrt(delta);
    var closest = (f.LengthSquared() - r2)/q;
    if (closest > Epsilon)
    {
      return closest;
    }
    if (q > Epsilon)
    {
      return q;
    }
    // Too shallow, strange cases...
    return 0;
  }

  public static TracerResult ComputePayload(TracerPayload payload,
                                            int           nThreads)
  {
    if (payload.ImgHeight <= 0 || payload.ImgWidth <= 0 || payload.TaskHeight <= 0 || payload.TaskWidth <= 0 || payload.Samples <= 0 || payload.CoordX < 0 ||
        payload.CoordY    < 0)
    {
      throw new ArgumentException("Bad payload");
    }
    // X increment to go from one pixel to the next
    var incX = new Vector3(payload.ImgWidth * payload.Camera.CST / payload.ImgHeight,
                               0,
                               0);
    var incY = payload.Camera.CST * Fast.Normalize(Vector3.Cross(incX,
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
                   var pixelRadiance = new Vector3(0,0,0);

                   for (var s = 0; s < payload.Samples; s++)
                   {
                     var rd = 2 * Xoshiro.next_float2(state);
                     //var randY = 2 * Xoshiro.next_double(state);
                     var dx = ((rd.X < 1) ? Fast.Sqrt(rd.X) - 1 : 1 - Fast.Sqrt(2 - rd.X));
                     var dy = ((rd.Y < 1) ? Fast.Sqrt(rd.Y) - 1 : 1 - Fast.Sqrt(2 - rd.Y));
                     var rayDirection = Fast.Normalize(payload.Camera.Direction +
                                                          (((1 + dy) * 0.5f + i) / payload.ImgHeight - 0.5f) * incY +
                                                          (((1 + dx) * 0.5f + j) / payload.ImgWidth  - 0.5f) * incX);
                     var rayOrigin = payload.Camera.Position + payload.Camera.Length * rayDirection;
                     pixelRadiance += sampleWeight * Radiance(payload, rayOrigin, rayDirection, 0, state);
                   }

                   pixelRadiance = Vector3.Clamp(pixelRadiance, Vector3.Zero, Vector3.One);
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
