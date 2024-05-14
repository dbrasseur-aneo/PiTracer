// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
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


using System.Numerics;

namespace PiTracerLib.ImageQuality;

public class QSSIM : ImageQualityMetric
{
  public const float K1           = 0.01f;
  public const float K2           = 0.03f;
  public const float Gaussian_Std = 1.5f;

  private static readonly float[,] Weights = createGaussian(Gaussian_Std, Gaussian_Std);

  /**
   * Creates a gaussian window
   * @param stdX Standard deviation in the X direction
   * @param stdY Standard deviation in the Y direction
   * @return 2D Gaussian window
   */
  private static float[,] createGaussian(float stdX,
                                         float stdY)
  {
    var rawX   = createGaussian1D(stdX);
    var rawY   = createGaussian1D(stdY);
    var size   = rawX.Length > rawY.Length ? rawX.Length : rawY.Length;
    var kernel = new float[size, size];

    if (size > rawX.Length)
    {
      // Center 1D filter
      rawX = center1D(rawX, size);
    }
    else if (size > rawY.Length)
    {
      rawY = center1D(rawY, size);
    }

    float sum = 0;
    for (var i = 0; i < rawY.Length; i++)
    {
      for (var j = 0; j < rawX.Length; j++)
      {
        kernel[i, j] =  rawY[i] * rawX[j];
        sum          += kernel[i, j];
      }
    }

    for (var i = 0; i < rawY.Length; i++)
    {
      for (var j = 0; j < rawX.Length; j++)
      {
        kernel[i, j] /= sum;
      }
    }

    return kernel;
  }

  /**
   * Center a 1D array
   * @param arr Input array
   * @param size new size for the array
   * @return 0 padded array of the input array
   */
  private static float[] center1D(float[] arr,
                                  int     size)
  {
    var pad     = (size - arr.Length) / 2;
    var newRawX = new float[size];
    for (var i = 0; i < size; i++)
    {
      if (i - pad < 0 || i - pad >= arr.Length)
      {
        newRawX[i] = 0;
      }
      else
      {
        newRawX[i] = arr[i - pad];
      }
    }

    return newRawX;
  }

  private static float[] createGaussian1D(float std)
  {
    if (std < 1.0e-10)
    {
      return new float[]
             {
               1,
             };
    }

    var sigma2 = std * std;
    var k      = (int)MathF.Ceiling(std * 3.0f);

    var width = 2 * k + 1;

    var   kernel = new float[width];
    float sum    = 0;
    for (var i = -k; i <= k; i++)
    {
      sum += kernel[i + k] = 1.0f / (Fast.Sqrt((float)(2 * Math.PI)) * std * MathF.Exp(i * i / sigma2 * 0.5f));
    }

    for (var i = 0; i < kernel.Length; i++)
    {
      kernel[i] /= sum;
    }

    return kernel;
  }


  public override float[,] GetMetricMap(ICollection<Vector3> linearData,
                                        ICollection<Vector3> refData,
                                        (int, int)?          dataSize = null)
    => throw new NotImplementedException();

  public override float GetMeanMetric(ICollection<Vector3> linearData,
                                      ICollection<Vector3> refData,
                                      (int, int)?          dataSize = null)
    => throw new NotImplementedException();

  public override float GetMeanMetric(Span<byte> linearData,
                                      Span<byte> refData)
    => throw new NotImplementedException();
}
