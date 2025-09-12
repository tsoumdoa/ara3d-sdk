namespace Ara3D.Studio.Samples;

public class TestNodes : IModelModifier
{
    [Range(0, 20)]
    public int ObjectComplexity = 2;

    public int MinTriangleCount => (int)Math.Pow(ObjectComplexity, 2);

    public Model3D Eval(Model3D model3D, EvalContext context)
    {
        var meshIndexes = model3D.Meshes.IndicesWhere(m => m.Triangles.Count > MinTriangleCount).ToHashSet();
        Debug.WriteLine($"Found {meshIndexes.Count} meshes with more triangles than {MinTriangleCount}");
        return model3D.WithStructs(model3D.ElementStructs.Where(es => meshIndexes.Contains(es.MeshIndex)).ToList());
    }
}