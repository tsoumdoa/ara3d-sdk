namespace Ara3D.Studio.Samples;

public class WallDemo : IModelGenerator
{
    [Range(0f, 2f)] public float Thickness = 0.1f;
    [Range(0f, 10f)] public float Height = 2;
    [Range(0f, 10f)] public float Radius = 2;
    [Range(2, 20)] public int Count = 5;
    
    public Model3D Eval(EvalContext ctx)
    {
        var mesh = PlatonicSolids.TriangulatedCube;
        var pts = Polygons.CirclePoints(Count).Select(pt => pt * Radius);
        var transforms = new List<Matrix4x4>();
        for (var i = 0; i < pts.Count; i++)
        {
            var line = new Line3D(pts[i].To3D, pts.ElementAtModulo(i + 1).To3D);
            var transform = line.ToBoxTransform(Thickness, Height);
            transforms.Add(transform);
        }

        return Model3D.Create(mesh, Material.Default, transforms);
    }
}