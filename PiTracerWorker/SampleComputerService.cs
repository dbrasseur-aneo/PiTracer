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

  int toInt(double x)
  {
      return (int)(Math.Pow(x, 1 / 2.2) * 255 + .5);   /* gamma correction = 2.2 */
  }

  double sphere_intersect(Sphere s, double[] ray_origin, double[] ray_direction)
    { 
	    double[] op={0,0,0};
	    // Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0 
	    copy(s.Position, op);
	    axpy(-1, ray_origin, op);
	    double eps = 1e-4;
	    double b = dot(op, ray_direction);
	    double discriminant = b * b - dot(op, op) + s.Radius * s.Radius; 
	    if (discriminant < 0)
      {
        return 0; /* pas d'intersection */
      }
      else
      {
        discriminant = Math.Sqrt(discriminant);
      }

      /* d�termine la plus petite solution positive (i.e. point d'intersection le plus proche, mais devant nous) */
	    double t = b - discriminant;
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

    /* d�termine si le rayon intersecte l'une des spere; si oui renvoie true et fixe t, id */
    bool intersect(IList<Sphere> spheres, double[] ray_origin, double[] ray_direction, out double t,out int id)
    { 
	    int n = spheres.Count;
	    double inf = 1e20; 
	    t = inf;
        id = -1;
	    for (int i = 0; i < n; i++) {
		    double d = sphere_intersect(spheres[i], ray_origin, ray_direction);
		    if ((d > 0) && (d < t)) {
			    t = d;
			    id = i;
		    } 
	    }
	    return t < inf;
    } 

    /* calcule (dans out) la lumiance re�ue par la camera sur le rayon donn� */
    void radiance(TracerPayload payload, double[] ray_origin, double[] ray_direction, int depth, ulong[] state, double[] rad)
    { 
	    int id = 0;                             // id de la sph�re intersect�e par le rayon
	    double t;                               // distance � l'intersection
	    if (!intersect(payload.Spheres, ray_origin, ray_direction, out t, out id)) {
		    zero(rad);    // if miss, return black 
		    return; 
	    }
	    Sphere obj = payload.Spheres[id];
	    
	    /* point d'intersection du rayon et de la sph�re */
	    double[] x = {0,0,0};
	    copy(ray_origin, x);
	    axpy(t, ray_direction, x);
	    
	    /* vecteur normal � la sphere, au point d'intersection */
	    double[] n={0,0,0};  
	    copy(x, n);
	    axpy(-1, obj.Position, n);
	    normalize(n);
	    
	    /* vecteur normal, orient� dans le sens oppos� au rayon 
	       (vers l'ext�rieur si le rayon entre, vers l'int�rieur s'il sort) */
	    double[] nl={0,0,0};
	    copy(n, nl);
	    if (dot(n, ray_direction) > 0)
		    scal(-1, nl);
	    
	    /* couleur de la sphere */
	    double[] f={0,0,0};
	    copy(obj.Color, f);
	    double p = obj.MaxReflexivity;

	    /* processus al�atoire : au-del� d'une certaine profondeur,
	       d�cide al�atoirement d'arr�ter la r�cusion. Plus l'objet est
	       clair, plus le processus a de chance de continuer. */
	    depth++;
	    if (depth > payload.KillDepth) {
		    if (Xoshiro.next_double(state) < p) {
			    scal(1 / p, f); 
		    } else {
			    copy(obj.Emission, rad);
			    return;
		    }
	    }

	    /* Cas de la r�flection DIFFuse (= non-brillante). 
	       On r�cup�re la luminance en provenance de l'ensemble de l'univers. 
	       Pour cela : (processus de monte-carlo) on choisit une direction
	       al�atoire dans un certain cone, et on r�cup�re la luminance en 
	       provenance de cette direction. */
	    if (obj.Refl == (int)Reflection.Diff) {
		    double r1 = 2 * Math.PI * Xoshiro.next_double(state);  /* angle al�atoire */
		    double r2 = Xoshiro.next_double(state);             /* distance au centre al�atoire */
		    double r2s = Math.Sqrt(r2); 
		    
		    double[] w={0,0,0};   /* vecteur normal */
		    copy(nl, w);
		    
		    double[] u = {0,0,0};   /* u est orthogonal � w */
		    double[] uw = {0, 0, 0};
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
		    
		    double[] v={0,0,0};   /* v est orthogonal � u et w */
		    cross(w, u, v);
		    
		    double[] d={0,0,0};   /* d est le vecteur incident al�atoire, selon la bonne distribution */
		    zero(d);
		    axpy(Math.Cos(r1) * r2s, u, d);
		    axpy(Math.Sin(r1) * r2s, v, d);
		    axpy(Math.Sqrt(1 - r2), w, d);
		    normalize(d);
		    
		    /* calcule r�cursivement la luminance du rayon incident */
		    double[] rec = {0,0,0};
		    radiance(payload, x, d, depth, state,  rec);
		    
		    /* pond�re par la couleur de la sph�re, prend en compte l'emissivit� */
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }

	    /* dans les deux autres cas (r�flection parfaite / refraction), on consid�re le rayon
	       r�fl�chi par la sp�re */

	    double[] reflected_dir = {0,0,0};
	    copy(ray_direction, reflected_dir);
	    axpy(-2 * dot(n, ray_direction), n, reflected_dir);

	    /* cas de la reflection SPEculaire parfaire (==mirroir) */
	    if (obj.Refl == (int)Reflection.Spec) { 
		    double[] rec = {0,0,0};
		    /* calcule r�cursivement la luminance du rayon r�flechi */
		    radiance(payload, x, reflected_dir, depth, state, rec);
		    /* pond�re par la couleur de la sph�re, prend en compte l'emissivit� */
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }

	    /* cas des surfaces di�lectriques (==verre). Combinaison de r�flection et de r�fraction. */
	    bool into = dot(n, nl) > 0;      /* vient-il de l'ext�rieur ? */
	    double nc = 1;                   /* indice de r�fraction de l'air */
	    double nt = 1.5;                 /* indice de r�fraction du verre */
	    double nnt = into ? (nc / nt) : (nt / nc);
	    double ddn = dot(ray_direction, nl);
	    
	    /* si le rayon essaye de sortir de l'objet en verre avec un angle incident trop faible,
	       il rebondit enti�rement */
	    double cos2t = 1 - nnt * nnt * (1 - ddn * ddn);
	    if (cos2t < 0) {
		    double[] rec = {0,0,0};
		    /* calcule seulement le rayon r�fl�chi */
		    radiance(payload, x, reflected_dir, depth, state, rec);
		    mul(f, rec, rad);
		    axpy(1, obj.Emission, rad);
		    return;
	    }
	    
	    /* calcule la direction du rayon r�fract� */
	    double[] tdir = {0,0,0};
	    zero(tdir);
	    axpy(nnt, ray_direction, tdir);
	    axpy(-(into ? 1 : -1) * (ddn * nnt + Math.Sqrt(cos2t)), n, tdir);

	    /* calcul de la r�flectance (==fraction de la lumi�re r�fl�chie) */
	    double a = nt - nc;
	    double b = nt + nc;
	    double R0 = a * a / (b * b);
	    double c = 1 - (into ? -ddn : dot(tdir, n));
	    double Re = R0 + (1 - R0) * c * c * c * c * c;   /* r�flectance */
	    double Tr = 1 - Re;                              /* transmittance */
	    
	    /* au-dela d'une certaine profondeur, on choisit al�atoirement si
	       on calcule le rayon r�fl�chi ou bien le rayon r�fract�. En dessous du
	       seuil, on calcule les deux. */
	    double[] recu = {0,0,0};
	    if (depth > payload.SplitDepth) {
		    double P = .25 + .5 * Re;             /* probabilit� de r�flection */
		    if (Xoshiro.next_double(state) < P) {
			    radiance(payload, x, reflected_dir, depth, state, recu);
			    double RP = Re / P;
			    scal(RP, recu);
		    } else {
			    radiance(payload, x, tdir, depth, state, recu);
			    double TP = Tr / (1 - P); 
			    scal(TP, recu);
		    }
	    } else {
		    double[] rec_re = {0,0,0};
            double[] rec_tr = {0,0,0};
		    radiance(payload, x, reflected_dir, depth, state, rec_re);
		    radiance(payload, x, tdir, depth, state, rec_tr);
		    zero(recu);
		    axpy(Re, rec_re, recu);
		    axpy(Tr, rec_tr, recu);
	    }
	    /* pond�re, prend en compte la luminance */
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

		double CST = 0.5135;  /* ceci d�fini l'angle de vue */
	    double[] camera_position = {50, 52, 295.6};
	    double[] camera_direction = {0, -0.042612, -1};
	    normalize(camera_direction);

        int w = payload.ImgWidth;
		int h = payload.ImgHeight;
        int samples = payload.Samples;

	    /* incr�ments pour passer d'un pixel � l'autre */
	    double[] cx = {w * CST / h, 0, 0};    
	    double[] cy={0,0,0};
	    cross(cx, camera_direction, cy);  /* cy est orthogonal � cx ET � la direction dans laquelle regarde la cam�ra */
	    normalize(cy);
	    scal(CST, cy);

	    /* pr�calcule la norme infinie des couleurs */
	    int n = payload.Spheres.Length;
	    for (int i = 0; i < n; i++) {
		    double[] f = payload.Spheres[i].Color;
            if ((f[0] > f[1]) && (f[0] > f[2]))
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
          MaxDegreeOfParallelism = 4
        };

        var rand = new Random();

        /* boucle principale */
        Parallel.For(0, payload.TaskHeight * payload.TaskWidth, options, offset =>
        {
          ulong[] state = {(ulong)rand.NextInt64(), (ulong)rand.NextInt64()};
          int i = payload.CoordX + (offset / payload.TaskWidth);
		  int j = payload.CoordY + (offset % payload.TaskWidth);
          /* calcule la luminance d'un pixel, avec sur-�chantillonnage 2x2 */
          double[] pixel_radiance = {0, 0, 0};
		  for (int s = 0; s < samples; s++) { 
                /* tire un rayon al�atoire dans une zone de la cam�ra qui correspond � peu pr�s au pixel � calculer */
            double r1 = 2 * Xoshiro.next_double(state);
            double dx = (r1 < 1) ? Math.Sqrt(r1) - 1 : 1 - Math.Sqrt(2 - r1); 
            double r2 = 2 * Xoshiro.next_double(state);
            double dy = (r2 < 1) ? Math.Sqrt(r2) - 1 : 1 - Math.Sqrt(2 - r2);
            double[] ray_direction = {0,0,0};
            copy(camera_direction, ray_direction);
            axpy(((1.0 + dy) / 2 + i) / h - .5, cy, ray_direction);
            axpy(((1.0 + dx) / 2 + j) / w - .5, cx, ray_direction);
            normalize(ray_direction);

            double[] ray_origin={0,0,0};
            copy(camera_position, ray_origin);
            axpy(140, ray_direction, ray_origin);
						
            /* estime la lumiance qui arrive sur la cam�ra par ce rayon */
            double[] sample_radiance = {0,0,0};
            radiance(payload, ray_origin, ray_direction, 0, state, sample_radiance);
            /* fait la moyenne sur tous les rayons */
            axpy(1.0/samples, sample_radiance, pixel_radiance);
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
