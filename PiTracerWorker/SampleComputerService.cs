// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using Microsoft.Extensions.Logging;
using static PiTracerWorker.SimpleBlas;

namespace PiTracerWorker;

public class SampleComputerService : WorkerStreamWrapper
{
  public SampleComputerService(ILoggerFactory      loggerFactory,
                               GrpcChannelProvider provider)
    : base(loggerFactory,
           provider)
    => logger_ = loggerFactory.CreateLogger<SampleComputerService>();

  int toInt(float x)
  {
      return (int)(Math.Pow(x, 1 / 2.2f) * 255 + .5f);   /* gamma correction = 2.2 */
  }

  float sphere_intersect(Sphere s, float[] ray_origin, float[] ray_direction)
    { 
	    float[] op={0,0,0};
	    // Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0 
	    copy(s.Position, op);
	    axpy(-1, ray_origin, op);
	    float eps = 1e-4f;
	    float b = dot(op, ray_direction);
	    float discriminant = b * b - dot(op, op) + s.Radius * s.Radius; 
	    if (discriminant < 0)
      {
        return 0; /* pas d'intersection */
      }
      else
      {
        discriminant = fast_sqrt(discriminant);
      }

      /* détermine la plus petite solution positive (i.e. point d'intersection le plus proche, mais devant nous) */
	    float t = b - discriminant;
	    if (t > eps) {
		    return t;
	    } else {
		    t = b + discriminant;
		    if (t > eps)
			    return t;
		    else
			    return 0;  /* cas bizarre, racine double, etc. */
	    }
    }

    /* détermine si le rayon intersecte l'une des spere; si oui renvoie true et fixe t, id */
    bool intersect(IList<Sphere> spheres, float[] ray_origin, float[] ray_direction, out float t,out int id)
    { 
	    int n = spheres.Count;
	    float inf = 1e20f; 
	    t = inf;
        id = -1;
	    for (int i = 0; i < n; i++) {
		    float d = sphere_intersect(spheres[i], ray_origin, ray_direction);
		    if ((d > 0) && (d < t)) {
			    t = d;
			    id = i;
		    } 
	    }
	    return t < inf;
    } 

    /* calcule (dans out) la lumiance reçue par la camera sur le rayon donné */
    void radiance(TracerPayload payload, float[] ray_origin, float[] ray_direction, int depth, ulong[] state, float[] rad)
    { 
	    int id = 0;                             // id de la sphère intersectée par le rayon
	    float t;                               // distance à l'intersection
	    if (!intersect(payload.Spheres, ray_origin, ray_direction, out t, out id)) {
		    zero(rad);    // if miss, return black 
		    return; 
	    }
	    Sphere obj = payload.Spheres[id];
	    
	    /* point d'intersection du rayon et de la sphère */
	    float[] x = {0,0,0};
	    copy(ray_origin, x);
	    axpy(t, ray_direction, x);
	    
	    /* vecteur normal à la sphere, au point d'intersection */
	    float[] n={0,0,0};  
	    copy(x, n);
	    axpy(-1, obj.Position, n);
	    normalize(n);
	    
	    /* vecteur normal, orienté dans le sens opposé au rayon 
	       (vers l'extérieur si le rayon entre, vers l'intérieur s'il sort) */
	    float[] nl={0,0,0};
	    copy(n, nl);
	    if (dot(n, ray_direction) > 0)
		    scal(-1, nl);
	    
	    /* couleur de la sphere */
	    float[] f={0,0,0};
	    copy(obj.Color, f);
	    float p = obj.MaxReflexivity;

	    /* processus aléatoire : au-delà d'une certaine profondeur,
	       décide aléatoirement d'arrêter la récusion. Plus l'objet est
	       clair, plus le processus a de chance de continuer. */
	    depth++;
	    if (depth > payload.KillDepth) {
		    if (Xoshiro.next_float(state) < p) {
			    scal(1 / p, f); 
		    } else {
			    copy(obj.Emission, rad);
			    return;
		    }
	    }

	    /* Cas de la réflection DIFFuse (= non-brillante). 
	       On récupère la luminance en provenance de l'ensemble de l'univers. 
	       Pour cela : (processus de monte-carlo) on choisit une direction
	       aléatoire dans un certain cone, et on récupère la luminance en 
	       provenance de cette direction. */
	    if (obj.Refl == (int)Reflection.Diff) {
		    float r1 = 2 * (float)Math.PI * Xoshiro.next_float(state);  /* angle aléatoire */
		    float r2 = Xoshiro.next_float(state);             /* distance au centre aléatoire */
		    float r2s = fast_sqrt(r2); 
		    
		    float[] w={0,0,0};   /* vecteur normal */
		    copy(nl, w);
		    
		    float[] u = {0,0,0};   /* u est orthogonal à w */
		    float[] uw = {0, 0, 0};
            if (Math.Abs(w[0]) > .1f)
            {
              uw[1] = 1;
            }
            else
            {
              uw[0] = 1;
            }
			    
		    cross(uw, w, u);
		    normalize(u);
		    
		    float[] v={0,0,0};   /* v est orthogonal à u et w */
		    cross(w, u, v);
		    
		    float[] d={0,0,0};   /* d est le vecteur incident aléatoire, selon la bonne distribution */
		    zero(d);
		    axpy((float)Math.Cos(r1) * r2s, u, d);
		    axpy((float)Math.Sin(r1) * r2s, v, d);
		    axpy(fast_sqrt(1 - r2),         w, d);
		    normalize(d);
		    
		    /* calcule récursivement la luminance du rayon incident */
		    float[] rec = {0,0,0};
		    radiance(payload, x, d, depth, state,  rec);
		    
		    /* pondère par la couleur de la sphère, prend en compte l'emissivité */
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }

	    /* dans les deux autres cas (réflection parfaite / refraction), on considère le rayon
	       réfléchi par la spère */

	    float[] reflected_dir = {0,0,0};
	    copy(ray_direction, reflected_dir);
	    axpy(-2 * dot(n, ray_direction), n, reflected_dir);

	    /* cas de la reflection SPEculaire parfaire (==mirroir) */
	    if (obj.Refl == (int)Reflection.Spec) { 
		    float[] rec = {0,0,0};
		    /* calcule récursivement la luminance du rayon réflechi */
		    radiance(payload, x, reflected_dir, depth, state, rec);
		    /* pondère par la couleur de la sphère, prend en compte l'emissivité */
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }

	    /* cas des surfaces diélectriques (==verre). Combinaison de réflection et de réfraction. */
	    bool into = dot(n, nl) > 0;      /* vient-il de l'extérieur ? */
	    float nc = 1;                   /* indice de réfraction de l'air */
	    float nt = 1.5f;                 /* indice de réfraction du verre */
	    float nnt = into ? (nc / nt) : (nt / nc);
	    float ddn = dot(ray_direction, nl);
	    
	    /* si le rayon essaye de sortir de l'objet en verre avec un angle incident trop faible,
	       il rebondit entièrement */
	    float cos2t = 1 - nnt * nnt * (1 - ddn * ddn);
	    if (cos2t < 0) {
		    float[] rec = {0,0,0};
		    /* calcule seulement le rayon réfléchi */
		    radiance(payload, x, reflected_dir, depth, state, rec);
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }
	    
	    /* calcule la direction du rayon réfracté */
	    float[] tdir = {0,0,0};
	    zero(tdir);
	    axpy(nnt, ray_direction, tdir);
	    axpy(-(into ? 1 : -1) * (ddn * nnt + fast_sqrt(cos2t)), n, tdir);

	    /* calcul de la réflectance (==fraction de la lumière réfléchie) */
	    float a  = nt - nc;
      float b  = nt + nc;
      float R0 = a * a / (b * b);
      float c  = 1  - (into ? -ddn : dot(tdir, n));
      float Re = R0 + (1 - R0) * c * c * c * c * c; /* réflectance */
      float Tr = 1  - Re;                           /* transmittance */
	    
	    /* au-dela d'une certaine profondeur, on choisit aléatoirement si
	       on calcule le rayon réfléchi ou bien le rayon réfracté. En dessous du
	       seuil, on calcule les deux. */
      float[] recu = {0,0,0};
	    if (depth > payload.SplitDepth) {
        float P = .25f + .5f * Re; /* probabilité de réflection */
		    if (Xoshiro.next_float(state) < P) {
			    radiance(payload, x, reflected_dir, depth, state, recu);
          float RP = Re / P;
			    scal(RP, recu);
		    } else {
			    radiance(payload, x, tdir, depth, state, recu);
          float TP = Tr / (1 - P); 
			    scal(TP, recu);
		    }
	    } else {
        float[] rec_re = {0,0,0};
        float[] rec_tr = {0,0,0};
		    radiance(payload, x, reflected_dir, depth, state, rec_re);
		    radiance(payload, x, tdir, depth, state, rec_tr);
		    zero(recu);
		    axpy(Re, rec_re, recu);
		    axpy(Tr, rec_tr, recu);
	    }
	    /* pondère, prend en compte la luminance */
	    mul(f, recu, rad);
	    axpy(1, obj.Emission, rad);
	    return;
    }

  public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      using var scopedLog = logger_.BeginNamedScope("Execute task",
                                                    ("Session", taskHandler.SessionId),
                                                    ("taskId", taskHandler.TaskId));
      var output = new Output();
      try
      {
        var payload = TracerPayload.deserialize(taskHandler.Payload, logger_);
        if (payload.TaskHeight <= 0 || payload.TaskWidth <= 0) throw new ArgumentException("Task size <= 0");

        var image = new byte[payload.TaskHeight * payload.TaskWidth * 3];

        float   CST              = 0.5135f; /* ceci défini l'angle de vue */
        float[] camera_position  = {50, 52, 295.6f};
        float[] camera_direction = {0, -0.042612f, -1};
	    normalize(camera_direction);

        int w = payload.ImgWidth;
		int h = payload.ImgHeight;
        int samples = payload.Samples;

	    /* incréments pour passer d'un pixel à l'autre */
      float[] cx = {w * CST / h, 0, 0};    
      float[] cy ={0,0,0};
	    cross(cx, camera_direction, cy);  /* cy est orthogonal à cx ET à la direction dans laquelle regarde la caméra */
	    normalize(cy);
	    scal(CST, cy);

	    /* précalcule la norme infinie des couleurs */
	    int n = payload.Spheres.Length;
	    for (int i = 0; i < n; i++) {
        float[] f = payload.Spheres[i].Color;
            if ((f[0] > f[1]) && (f[0] > f[2]))
            {
              payload.Spheres[i].MaxReflexivity = f[0]; 
            }
            else
            {
              payload.Spheres[i].MaxReflexivity = f[1] > f[2] ? f[1] : f[2];
            }
	    }

      ParallelOptions options;
      try
      {
        options = new ParallelOptions
                  {
                    MaxDegreeOfParallelism = taskHandler.TaskOptions.Options.ContainsKey("nThreads")
                                               ? int.Parse(taskHandler.TaskOptions.Options["nThreads"])
                                               : 8,
                  };
      }
      catch (Exception ex)
      {
        logger_.LogWarning("Bad nThreads, using 8 instead");
        options = new ParallelOptions
                  {
                    MaxDegreeOfParallelism = 8,
                  };
      }

        var rand = new Random();

        /* boucle principale */
        Parallel.For(0, payload.TaskHeight * payload.TaskWidth, options, offset =>
        {
          ulong[] state = {(ulong)rand.NextInt64(), (ulong)rand.NextInt64()};
          int i = payload.CoordX + (offset / payload.TaskWidth);
		  int j = payload.CoordY + (offset % payload.TaskWidth);
          /* calcule la luminance d'un pixel, avec sur-échantillonnage 2x2 */
          float[] pixel_radiance = {0, 0, 0};
		  for (int s = 0; s < samples; s++) { 
                /* tire un rayon aléatoire dans une zone de la caméra qui correspond à peu près au pixel à calculer */
                float   r1            = 2 * Xoshiro.next_float(state);
                float   dx            = (r1 < 1) ? fast_sqrt(r1) - 1 : 1 - fast_sqrt(2 - r1); 
                float   r2            = 2 * Xoshiro.next_float(state);
                float   dy            = (r2 < 1) ? fast_sqrt(r2) - 1 : 1 - fast_sqrt(2 - r2);
                float[] ray_direction = {0,0,0};
            copy(camera_direction, ray_direction);
            axpy(((1.0f + dy) / 2 + i) / h - .5f, cy, ray_direction);
            axpy(((1.0f + dx) / 2 + j) / w - .5f, cx, ray_direction);
            normalize(ray_direction);

            float[] ray_origin={0,0,0};
            copy(camera_position, ray_origin);
            axpy(140, ray_direction, ray_origin);
						
            /* estime la lumiance qui arrive sur la caméra par ce rayon */
            float[] sample_radiance = {0,0,0};
            radiance(payload, ray_origin, ray_direction, 0, state, sample_radiance);
            /* fait la moyenne sur tous les rayons */
            axpy(1.0f/samples, sample_radiance, pixel_radiance);
          }
		      clamp(pixel_radiance);
          var index = offset * 3;
          //BGR instead of RGB
          image[index] = (byte) toInt(pixel_radiance[2]);
          image[index+1] = (byte) toInt(pixel_radiance[1]);
          image[index+2] = (byte) toInt(pixel_radiance[0]);
        });

        var reply = new TracerResult()
        {
          CoordX     = payload.CoordX,
          CoordY     = payload.CoordY,
          TaskHeight = payload.TaskHeight,
          TaskWidth  = payload.TaskWidth,
          Pixels     = image,
        };

        await taskHandler.SendResult(taskHandler.ExpectedResults.Single(),
                                     reply.serialize());

        output = new Output
        {
          Ok     = new Empty(),
        };
      }
      catch (Exception ex)
      {
        logger_.LogError(ex,
                         "Error while computing task");

        output = new Output
        {
          Error = new Output.Types.Error
          {
            Details = ex.Message + ex.StackTrace
          }
        };
      }
      return output;
    }
}
