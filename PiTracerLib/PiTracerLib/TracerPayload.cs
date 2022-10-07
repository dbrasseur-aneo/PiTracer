namespace PiTracerLib;

public readonly struct TracerPayload
{
  public int      ImgWidth   { get; }
  public int      ImgHeight  { get; }
  public int      CoordX     { get; }
  public int      CoordY     { get; }
  public int      KillDepth  { get; }
  public int      SplitDepth { get; }
  public int      TaskWidth  { get; }
  public int      TaskHeight { get; }
  public int      Samples    { get; }
  public Camera   Camera     { get; }
  public Sphere[] Spheres    { get; }

  public TracerPayload(byte[] payload)
  {
    ImgWidth = BitConverter.ToInt32(payload,
                                    0);
    ImgHeight = BitConverter.ToInt32(payload,
                                     4);
    CoordX = BitConverter.ToInt32(payload,
                                  8);
    CoordY = BitConverter.ToInt32(payload,
                                  12);
    KillDepth = BitConverter.ToInt32(payload,
                                     16);
    SplitDepth = BitConverter.ToInt32(payload,
                                      20);
    TaskWidth = BitConverter.ToInt32(payload,
                                     24);
    TaskHeight = BitConverter.ToInt32(payload,
                                      32);
    Samples = BitConverter.ToInt32(payload,
                                   36);
    var offset = 36 + 4;
    Camera = new Camera(payload,
                        offset);
    offset  += Camera.Size();
    Spheres =  new Sphere[(payload.Length - offset) / Sphere.Size];
    for (var i = 0; i < Spheres.Length; i++)
    {
      Spheres[i] = new Sphere(payload,
                              offset + Sphere.Size * i);
    }
  }
}
