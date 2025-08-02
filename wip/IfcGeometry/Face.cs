using System.Numerics;

namespace Ara3D.IfcGeometry;

public class Bounds
{
    public bool Direction;
    public List<Vector3> Points = new();
}

public class Face
{
    public List<Bounds> Bounds = new();
}

public class FaceSet
{
    public List<Face> Faces = new();
}