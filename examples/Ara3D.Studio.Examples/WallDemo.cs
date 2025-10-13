namespace Ara3D.Studio.Samples;

public class WallDemo : IModelGenerator
{
    [Range(0f, 2f)] public float Thickness = 0.1f;
    [Range(0f, 10f)] public float Height = 2;
    [Range(0f, 10f)] public float Radius = 2;
    [Range(2, 20)] public int Count = 5;
    
    public Model3D Eval(EvalContext ctx)
    {
        var mb = new Model3DBuilder();
        mb.Meshes.Add(PlatonicSolids.TriangulatedCube);
        mb.Materials.Add(mb.DefaultMaterial);
        var pts = Polygons.CirclePoints(Count).Select(pt => pt * Radius);
        for (var i = 0; i < pts.Count; i++)
        {
            var line = new Line3D(pts[i].To3D, pts.ElementAtModulo(i + 1).To3D);
            var transform = line.ToBoxTransform(Thickness, Height);
            var es = new ElementStruct(0, 0, 0, mb.AddTransform(transform));
            mb.ElementStructs.Add(es);
        }
        return mb.Build();
    }
}