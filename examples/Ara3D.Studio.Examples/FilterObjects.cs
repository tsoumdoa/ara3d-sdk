namespace Ara3D.Studio.Samples;

public class FilterObjects : IModelModifier
{
    [Range(0, 20)] public int ObjectComplexity = 2;

    public int MinTriangleCount => ObjectComplexity * ObjectComplexity * ObjectComplexity;

    public Model3D Eval(Model3D model3D, EvalContext context)
        => model3D.FilterMeshes(mesh => mesh.Triangles.Count >= MinTriangleCount);
}