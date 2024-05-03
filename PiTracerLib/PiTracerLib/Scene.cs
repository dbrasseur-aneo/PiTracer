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

namespace PiTracerLib
{
  public class Scene
  {
    public int        ImgWidth   { get; }
    public int        ImgHeight  { get; }
    public int        KillDepth  { get; }
    public int        SplitDepth { get; }
    public Camera     Camera     { get; }
    public SphereList Spheres    { get; }

    public Scene(byte[] payload)
    {
      ImgWidth = BitConverter.ToInt32(payload, 0);
      ImgHeight = BitConverter.ToInt32(payload, 4);
      KillDepth = BitConverter.ToInt32(payload, 8);
      SplitDepth = BitConverter.ToInt32(payload, 12);
      var offset = 12 + 4;
      Camera = new Camera(payload, ImgWidth, ImgHeight, offset);
      offset += Camera.Size;
      var spheres = new Sphere[(payload.Length - offset) / Sphere.Size];
      for (var i = 0; i < spheres.Length; i++)
      {
        spheres[i] = new Sphere(payload, offset + Sphere.Size * i);
      }

      Spheres = new SphereList(spheres);
    }

    public Scene(int imgWidth,
                         int imgHeight,
                         int killDepth,
                         int splitDepth,
                         Camera camera,
                         Sphere[] spheres)
    {
      ImgWidth = imgWidth;
      ImgHeight = imgHeight;
      KillDepth = killDepth;
      SplitDepth = splitDepth;
      Camera = camera;
      Spheres = new SphereList(spheres);
    }

    public byte[] ToBytes()
    {
      var bytes = new byte[36 + Camera.Size + Spheres.Length * Sphere.Size];
      BitConverter.GetBytes(ImgWidth).CopyTo(bytes, 0);
      BitConverter.GetBytes(ImgHeight).CopyTo(bytes, 4);
      BitConverter.GetBytes(KillDepth).CopyTo(bytes, 8);
      BitConverter.GetBytes(SplitDepth).CopyTo(bytes, 12);
      Camera.ToBytes().CopyTo(bytes, 16);
      for (var i = 0; i < Spheres.Length; i++)
      {
        Spheres[i].ToBytes().CopyTo(bytes, 16 + Camera.Size + i * Sphere.Size);
      }

      return bytes;
    }
  }
}
