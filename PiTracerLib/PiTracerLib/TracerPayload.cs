namespace PiTracerLib;

public readonly struct TracerPayload
{
  public int        ImgWidth   { get; }
  public int        ImgHeight  { get; }
  public int        CoordX     { get; }
  public int        CoordY     { get; }
  public int        KillDepth  { get; }
  public int        SplitDepth { get; }
  public int        TaskWidth  { get; }
  public int        TaskHeight { get; }
  public int        Samples    { get; }
  public Camera     Camera     { get; }
  public SphereList Spheres    { get; }

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
                                      28);
    Samples = BitConverter.ToInt32(payload,
                                   32);
    var offset = 32 + 4;
    Camera =  new Camera(payload,
                         offset);
    offset += Camera.Size;
    var spheres = new Sphere[(payload.Length - offset) / Sphere.Size];
    for (var i = 0; i < spheres.Length; i++)
    {
      spheres[i] = new Sphere(payload, offset + Sphere.Size * i);
    }

    Spheres = new SphereList(spheres);
  }

  public TracerPayload(int      imgWidth,
                       int      imgHeight,
                       int      coordX,
                       int      coordY,
                       int      killDepth,
                       int      splitDepth,
                       int      taskWidth,
                       int      taskHeight,
                       int      samples,
                       Camera   camera,
                       Sphere[] spheres)
  {
    ImgWidth   = imgWidth;
    ImgHeight  = imgHeight;
    CoordX     = coordX;
    CoordY     = coordY;
    KillDepth  = killDepth;
    SplitDepth = splitDepth;
    TaskWidth  = taskWidth;
    TaskHeight = taskHeight;
    Samples    = samples;
    Camera     = camera;
    Spheres    = new SphereList(spheres);
  }

  public byte[] ToBytes()
  {
    var bytes = new byte[36 + Camera.Size + Spheres.Length * Sphere.Size];
    BitConverter.GetBytes(ImgWidth).CopyTo(bytes, 0);
    BitConverter.GetBytes(ImgHeight).CopyTo(bytes, 4);
    BitConverter.GetBytes(CoordX).CopyTo(bytes, 8);
    BitConverter.GetBytes(CoordY).CopyTo(bytes, 12);
    BitConverter.GetBytes(KillDepth).CopyTo(bytes, 16);
    BitConverter.GetBytes(SplitDepth).CopyTo(bytes, 20);
    BitConverter.GetBytes(TaskWidth).CopyTo(bytes, 24);
    BitConverter.GetBytes(TaskHeight).CopyTo(bytes, 28);
    BitConverter.GetBytes(Samples).CopyTo(bytes, 32);
    Camera.ToBytes().CopyTo(bytes, 36);
    for (var i = 0; i < Spheres.Length; i++)
    {
      Spheres[i].ToBytes().CopyTo(bytes, 36+Camera.Size + i*Sphere.Size);
    }

    return bytes;
  }
}
