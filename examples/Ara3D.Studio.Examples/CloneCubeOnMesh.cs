namespace Ara3D.Studio.Samples;

public class CloneCubeOnMesh : IModelModifier
{
    public bool AtFaceCenters;
    [Range(0f, 1f)] public float Scale = 0.1f;

    public Model3D Eval(Model3D m, EvalContext eval)
    {
        var material = m.FirstOrDefaultMaterial();
        var instancedMesh = PlatonicSolids.TriangulatedCube.Scale(Scale);
        var mergedMesh = m.ToMesh();
        var points = AtFaceCenters ? mergedMesh.Triangles.Map(f => f.Center) : mergedMesh.Points;
        return instancedMesh.Clone(material, points);
    }
}