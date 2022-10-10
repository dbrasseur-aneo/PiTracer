// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;
using ArmoniK.Samples.HtcMock.Adapter;
using ArmoniK.Samples.PiTracer.Adapter;

using Microsoft.Extensions.Logging;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Samples.HtcMock.GridWorker;

public class SampleComputerService : WorkerStreamWrapper
{
  [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used for side effects")]
  private readonly ApplicationLifeTimeManager applicationLifeTime_;

  private readonly ILogger<SampleComputerService> logger_;
  private readonly ILoggerFactory                 loggerFactory_;

  public SampleComputerService(ILoggerFactory             loggerFactory,
                               ApplicationLifeTimeManager applicationLifeTime)
    : base(loggerFactory)
  {
    logger_              = loggerFactory.CreateLogger<SampleComputerService>();
    loggerFactory_       = loggerFactory;
    applicationLifeTime_ = applicationLifeTime;
  }

  private static void copy(double[] x,
                           double[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] = x[i];
    }
  }

  private static void zero(double[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] = 0;
    }
  }

  private static void axpy(double   alpha,
                           double[] x,
                           double[] y)
  {
    for (var i = 0; i < 3; i++)
    {
      y[i] += alpha * x[i];
    }
  }

  private static void scal(double   alpha,
                           double[] x)
  {
    for (var i = 0; i < 3; i++)
    {
      x[i] *= alpha;
    }
  }

  private static double dot(double[] a,
                            double[] b)
    => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

  private static double nrm2(double[] a)
    => Math.Sqrt(dot(a, a));

  /********* fonction non-standard *************/
  private static void mul(double[] x,
                          double[] y,
                          double[] z)
  {
    for (var i = 0; i < 3; i++)
    {
      z[i] = x[i] * y[i];
    }
  }

  private static void normalize(double[] x)
    => scal(1 / nrm2(x), x);

  /* produit vectoriel */
  private static void cross(double[] a,
                            double[] b,
                            double[] c)
  {
    c[0] = a[1] * b[2] - a[2] * b[1];
    c[1] = a[2] * b[0] - a[0] * b[2];
    c[2] = a[0] * b[1] - a[1] * b[0];
  }

/****** tronque *************/
  private static void clamp(double[] x)
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

/******************************* calcul des intersections rayon / sphere *************************************/

// returns distance, 0 if nohit 
  private double sphere_intersect(Sphere   s,
                                  double[] ray_origin,
                                  double[] ray_direction)
  {
    double[] op =
    {
      0,
      0,
      0,
    };
    // Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0 
    copy(s.Position, op);
    axpy(-1, ray_origin, op);
    var eps          = 1e-4;
    var b            = dot(op, ray_direction);
    var discriminant = b * b - dot(op, op) + s.Radius * s.Radius;
    if (discriminant < 0)
    {
      return 0; /* pas d'intersection */
    }

    discriminant = Math.Sqrt(discriminant);

    /* détermine la plus petite solution positive (i.e. point d'intersection le plus proche, mais devant nous) */
    var t = b - discriminant;
    if (t > eps)
    {
      return t;
    }

    t = b + discriminant;
    if (t > eps)
    {
      return t;
    }

    return 0; /* cas bizarre, racine double, etc. */
  }

  /* détermine si le rayon intersecte l'une des spere; si oui renvoie true et fixe t, id */
  private bool intersect(IList<Sphere> spheres,
                         double[]      ray_origin,
                         double[]      ray_direction,
                         out double    t,
                         out int       id)
  {
    var n   = spheres.Count;
    var inf = 1e20;
    t  = inf;
    id = -1;
    for (var i = 0; i < n; i++)
    {
      var d = sphere_intersect(spheres[i], ray_origin, ray_direction);
      if (d > 0 && d < t)
      {
        t  = d;
        id = i;
      }
    }

    return t < inf;
  }

  /* calcule (dans out) la lumiance reçue par la camera sur le rayon donné */
  private void radiance(TracerPayload payload,
                        double[]      ray_origin,
                        double[]      ray_direction,
                        int           depth,
                        ulong[]       state,
                        double[]      rad)
  {
    var    id = 0; // id de la sphère intersectée par le rayon
    double t;      // distance à l'intersection
    if (!intersect(payload.Spheres, ray_origin, ray_direction, out t, out id))
    {
      zero(rad); // if miss, return black 
      return;
    }

    var obj = payload.Spheres[id];

    /* point d'intersection du rayon et de la sphère */
    double[] x =
    {
      0,
      0,
      0,
    };
    copy(ray_origin, x);
    axpy(t, ray_direction, x);

    /* vecteur normal à la sphere, au point d'intersection */
    double[] n =
    {
      0,
      0,
      0,
    };
    copy(x, n);
    axpy(-1, obj.Position, n);
    normalize(n);

    /* vecteur normal, orienté dans le sens opposé au rayon 
       (vers l'extérieur si le rayon entre, vers l'intérieur s'il sort) */
    double[] nl =
    {
      0,
      0,
      0,
    };
    copy(n, nl);
    if (dot(n, ray_direction) > 0)
    {
      scal(-1, nl);
    }

    /* couleur de la sphere */
    double[] f =
    {
      0,
      0,
      0,
    };
    copy(obj.Color, f);
    var p = obj.MaxReflexivity;

    /* processus aléatoire : au-delà d'une certaine profondeur,
       décide aléatoirement d'arrêter la récusion. Plus l'objet est
       clair, plus le processus a de chance de continuer. */
    depth++;
    if (depth > payload.KillDepth)
    {
      if (Xoshiro.next_double(state) < p)
      {
        scal(1 / p, f);
      }
      else
      {
        copy(obj.Emission, rad);
        return;
      }
    }

    /* Cas de la réflection DIFFuse (= non-brillante). 
       On récupère la luminance en provenance de l'ensemble de l'univers. 
       Pour cela : (processus de monte-carlo) on choisit une direction
       aléatoire dans un certain cone, et on récupère la luminance en 
       provenance de cette direction. */
    if (obj.Refl == (int)Reflection.Diff)
    {
      var r1  = 2 * Math.PI * Xoshiro.next_double(state); /* angle aléatoire */
      var r2  = Xoshiro.next_double(state);               /* distance au centre aléatoire */
      var r2s = Math.Sqrt(r2);

      double[] w =
      {
        0,
        0,
        0,
      }; /* vecteur normal */
      copy(nl, w);

      double[] u =
      {
        0,
        0,
        0,
      }; /* u est orthogonal à w */
      double[] uw =
      {
        0,
        0,
        0,
      };
      if (Math.Abs(w[0]) > .1)
      {
        uw[1] = 1;
      }
      else
      {
        uw[0] = 1;
      }

      cross(uw, w, u);
      normalize(u);

      double[] v =
      {
        0,
        0,
        0,
      }; /* v est orthogonal à u et w */
      cross(w, u, v);

      double[] d =
      {
        0,
        0,
        0,
      }; /* d est le vecteur incident aléatoire, selon la bonne distribution */
      zero(d);
      axpy(Math.Cos(r1) * r2s, u, d);
      axpy(Math.Sin(r1) * r2s, v, d);
      axpy(Math.Sqrt(1 - r2),  w, d);
      normalize(d);

      /* calcule récursivement la luminance du rayon incident */
      double[] rec =
      {
        0,
        0,
        0,
      };
      radiance(payload, x, d, depth, state, rec);

      /* pondère par la couleur de la sphère, prend en compte l'emissivité */
      mul(f, rec, rad);
      axpy(1, obj.Emission, rad);
      return;
    }

    /* dans les deux autres cas (réflection parfaite / refraction), on considère le rayon
       réfléchi par la spère */

    double[] reflected_dir =
    {
      0,
      0,
      0,
    };
    copy(ray_direction, reflected_dir);
    axpy(-2 * dot(n, ray_direction), n, reflected_dir);

    /* cas de la reflection SPEculaire parfaire (==mirroir) */
    if (obj.Refl == (int)Reflection.Spec)
    {
      double[] rec =
      {
        0,
        0,
        0,
      };
      /* calcule récursivement la luminance du rayon réflechi */
      radiance(payload, x, reflected_dir, depth, state, rec);
      /* pondère par la couleur de la sphère, prend en compte l'emissivité */
      mul(f, rec, rad);
      axpy(1, obj.Emission, rad);
      return;
    }

    /* cas des surfaces diélectriques (==verre). Combinaison de réflection et de réfraction. */
    var    into = dot(n, nl) > 0; /* vient-il de l'extérieur ? */
    double nc   = 1;              /* indice de réfraction de l'air */
    var    nt   = 1.5;            /* indice de réfraction du verre */
    var    nnt  = into ? nc / nt : nt / nc;
    var    ddn  = dot(ray_direction, nl);

    /* si le rayon essaye de sortir de l'objet en verre avec un angle incident trop faible,
       il rebondit entièrement */
    var cos2t = 1 - nnt * nnt * (1 - ddn * ddn);
    if (cos2t < 0)
    {
      double[] rec =
      {
        0,
        0,
        0,
      };
      /* calcule seulement le rayon réfléchi */
      radiance(payload, x, reflected_dir, depth, state, rec);
      mul(f, rec, rad);
      axpy(1, obj.Emission, rad);
      return;
    }

    /* calcule la direction du rayon réfracté */
    double[] tdir =
    {
      0,
      0,
      0,
    };
    zero(tdir);
    axpy(nnt,                                               ray_direction, tdir);
    axpy(-(into ? 1 : -1) * (ddn * nnt + Math.Sqrt(cos2t)), n,             tdir);

    /* calcul de la réflectance (==fraction de la lumière réfléchie) */
    var a  = nt - nc;
    var b  = nt + nc;
    var R0 = a * a / (b * b);
    var c  = 1  - (into ? -ddn : dot(tdir, n));
    var Re = R0 + (1 - R0) * c * c * c * c * c; /* réflectance */
    var Tr = 1  - Re;                           /* transmittance */

    /* au-dela d'une certaine profondeur, on choisit aléatoirement si
       on calcule le rayon réfléchi ou bien le rayon réfracté. En dessous du
       seuil, on calcule les deux. */
    double[] recu =
    {
      0,
      0,
      0,
    };
    if (depth > payload.SplitDepth)
    {
      var P = .25 + .5 * Re; /* probabilité de réflection */
      if (Xoshiro.next_double(state) < P)
      {
        radiance(payload, x, reflected_dir, depth, state, recu);
        var RP = Re / P;
        scal(RP, recu);
      }
      else
      {
        radiance(payload, x, tdir, depth, state, recu);
        var TP = Tr / (1 - P);
        scal(TP, recu);
      }
    }
    else
    {
      double[] rec_re =
      {
        0,
        0,
        0,
      };
      double[] rec_tr =
      {
        0,
        0,
        0,
      };
      radiance(payload, x, reflected_dir, depth, state, rec_re);
      radiance(payload, x, tdir,          depth, state, rec_tr);
      zero(recu);
      axpy(Re, rec_re, recu);
      axpy(Tr, rec_tr, recu);
    }

    /* pondère, prend en compte la luminance */
    mul(f, recu, rad);
    axpy(1, obj.Emission, rad);
  }

  private int toInt(double x)
    => (int)(Math.Pow(x, 1 / 2.2) * 255 + .5) /* gamma correction = 2.2 */;

  public override async Task<Output> Process(ITaskHandler taskHandler)
  {
    using var scopedLog = logger_.BeginNamedScope("Execute task", ("Session", taskHandler.SessionId), ("taskId", taskHandler.TaskId));
    var       output    = new Output();
    try
    {
      var payload = TracerPayload.deserialize(taskHandler.Payload, logger_);
      if (payload.TaskHeight <= 0 || payload.TaskWidth <= 0)
      {
        throw new ArgumentException("Task size <= 0");
      }

      var image = new byte[payload.TaskHeight * payload.TaskWidth * 3];

      var CST = 0.5135; /* ceci défini l'angle de vue */
      double[] camera_position =
      {
        50,
        52,
        295.6,
      };
      double[] camera_direction =
      {
        0,
        -0.042612,
        -1,
      };
      normalize(camera_direction);

      var w       = payload.ImgWidth;
      var h       = payload.ImgHeight;
      var samples = payload.Samples;

      /* incréments pour passer d'un pixel à l'autre */
      double[] cx =
      {
        w * CST / h,
        0,
        0,
      };
      double[] cy =
      {
        0,
        0,
        0,
      };
      cross(cx, camera_direction, cy); /* cy est orthogonal à cx ET à la direction dans laquelle regarde la caméra */
      normalize(cy);
      scal(CST, cy);

      /* précalcule la norme infinie des couleurs */
      var n = payload.Spheres.Length;
      for (var i = 0; i < n; i++)
      {
        var f = payload.Spheres[i].Color;
        if (f[0] > f[1] && f[0] > f[2])
        {
          payload.Spheres[i].MaxReflexivity = f[0];
        }
        else
        {
          payload.Spheres[i].MaxReflexivity = f[1] > f[2] ? f[1] : f[2];
        }
      }

      var options = new ParallelOptions
                    {
                      MaxDegreeOfParallelism = 4,
                    };

      var rand = new Random();

      /* boucle principale */
      Parallel.For(0, payload.TaskHeight * payload.TaskWidth, options, offset =>
                                                                       {
                                                                         ulong[] state =
                                                                         {
                                                                           (ulong)rand.NextInt64(),
                                                                           (ulong)rand.NextInt64(),
                                                                         };
                                                                         var i = payload.CoordX + offset / payload.TaskWidth;
                                                                         var j = payload.CoordY + offset % payload.TaskWidth;
                                                                         /* calcule la luminance d'un pixel, avec sur-échantillonnage 2x2 */
                                                                         double[] pixel_radiance =
                                                                         {
                                                                           0,
                                                                           0,
                                                                           0,
                                                                         };
                                                                         for (var s = 0; s < samples; s++)
                                                                         {
                                                                           /* tire un rayon aléatoire dans une zone de la caméra qui correspond à peu près au pixel à calculer */
                                                                           var r1 = 2 * Xoshiro.next_double(state);
                                                                           var dx = r1 < 1 ? Math.Sqrt(r1) - 1 : 1 - Math.Sqrt(2 - r1);
                                                                           var r2 = 2 * Xoshiro.next_double(state);
                                                                           var dy = r2 < 1 ? Math.Sqrt(r2) - 1 : 1 - Math.Sqrt(2 - r2);
                                                                           double[] ray_direction =
                                                                           {
                                                                             0,
                                                                             0,
                                                                             0,
                                                                           };
                                                                           copy(camera_direction, ray_direction);
                                                                           axpy(((1.0 + dy) / 2 + i) / h - .5, cy, ray_direction);
                                                                           axpy(((1.0 + dx) / 2 + j) / w - .5, cx, ray_direction);
                                                                           normalize(ray_direction);

                                                                           double[] ray_origin =
                                                                           {
                                                                             0,
                                                                             0,
                                                                             0,
                                                                           };
                                                                           copy(camera_position, ray_origin);
                                                                           axpy(140, ray_direction, ray_origin);

                                                                           /* estime la lumiance qui arrive sur la caméra par ce rayon */
                                                                           double[] sample_radiance =
                                                                           {
                                                                             0,
                                                                             0,
                                                                             0,
                                                                           };
                                                                           radiance(payload, ray_origin, ray_direction, 0, state, sample_radiance);
                                                                           /* fait la moyenne sur tous les rayons */
                                                                           axpy(1.0 / samples, sample_radiance, pixel_radiance);
                                                                         }

                                                                         clamp(pixel_radiance);
                                                                         var index = offset * 3;
                                                                         //BGR instead of RGB
                                                                         image[index]     = (byte)toInt(pixel_radiance[2]);
                                                                         image[index + 1] = (byte)toInt(pixel_radiance[1]);
                                                                         image[index + 2] = (byte)toInt(pixel_radiance[0]);
                                                                       });

      var reply = new TracerResult
                  {
                    CoordX     = payload.CoordX,
                    CoordY     = payload.CoordY,
                    TaskHeight = payload.TaskHeight,
                    TaskWidth  = payload.TaskWidth,
                    Pixels     = image,
                  };

      await taskHandler.SendResult(taskHandler.ExpectedResults.Single(), reply.serialize());

      output = new Output
               {
                 Ok     = new Empty(),
                 Status = TaskStatus.Completed,
               };
    }
    catch (Exception ex)
    {
      logger_.LogError(ex, "Error while computing task");

      output = new Output
               {
                 Error = new Output.Types.Error
                         {
                           Details      = ex.Message + ex.StackTrace,
                           KillSubTasks = true,
                         },
                 Status = TaskStatus.Error,
               };
    }

    return output;
  }
}
