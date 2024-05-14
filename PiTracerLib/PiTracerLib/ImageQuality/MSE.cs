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

// ReSharper disable once InconsistentNaming
public class MSE : ImageQualityMetric
{
  public override float[,] GetMetricMap(ICollection<Vector3> linearData,
                                        ICollection<Vector3> refData,
                                        (int, int)?          dataSize = null)
  {
    dataSize ??= (linearData.Count, 1);
    var result = new float[dataSize.Value.Item1, dataSize.Value.Item2];
    for (var i = 0; i < dataSize.Value.Item1; i++)
    {
      for (var j = 0; j < dataSize.Value.Item2; j++)
      {
        var index = i * dataSize.Value.Item2    + j;
        var diff  = linearData.ElementAt(index) - refData.ElementAt(index);
        result[i, j] = (diff * diff).LengthSquared();
      }
    }

    return result;
  }

  public override float GetMeanMetric(ICollection<Vector3> linearData,
                                      ICollection<Vector3> refData,
                                      (int, int)?          dataSize = null)
    => GetMetricMap(linearData, refData, dataSize).Cast<float>().Average();

  public override float GetMeanMetric(Span<byte> linearData,
                                      Span<byte> refData)
  {
    var cardinal    = Vector<float>.Count;
    var sliceSize   = cardinal          * sizeof(float);
    var fullVectors = linearData.Length / sliceSize;
    var nFloats     = linearData.Length / sizeof(float);
    var acc         = Vector<float>.Zero;
    var index       = 0;
    for (var i = 0; i < fullVectors; i++)
    {
      var diff = 255* new Vector<float>(linearData.Slice(index, sliceSize)) - 255* new Vector<float>(refData.Slice(index, sliceSize));
      acc   += diff     * diff;
      index += sliceSize;
    }

    var mean = Vector.Sum(acc);

    for (var i = fullVectors * sliceSize; i < linearData.Length; i+=4)
    {
      var diff = BitConverter.ToSingle(linearData[i..(i+4)]) - BitConverter.ToSingle(refData[i..(i+4)]);
      mean += diff * diff;
    }
    return mean/nFloats;

  }
}
