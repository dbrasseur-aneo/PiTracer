using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.Samples.PiTracer.Adapter
{
  public class Xoshiro
  {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct Union
    {
      [FieldOffset(0)]
      public double d;
      [FieldOffset(0)]
      public ulong l;
    }
    
    public static double next_double(ulong[] s) {
      ulong s0 = s[0];
      ulong s1 = s[1];
      ulong result = s0 + s1;

      s1 ^= s0;
      s[0] = ((s0 << 24) | (s0 >> (64 - 24))) ^ s1 ^ (s1 << 16); // a, b
      s[1] = (s1 << 37) | (s1 >> (64 - 37)); // c

      var u = new Union
      {
        l=0x3FF0000000000000 | (result >> 12)
      };
      return u.d-1;
    }
  }
}
