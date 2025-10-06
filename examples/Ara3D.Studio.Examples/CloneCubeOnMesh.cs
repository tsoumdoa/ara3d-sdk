namespace Ara3D.Studio.Samples;

public class CloneCubeOnMesh : IModelModifier
{
    public bool AtFaceCenters;
    [Range(0f, 1f)] public float Scale = 0.1f;

    public static Element ToElement(TriangleMesh3D mesh, Point3D position, Material mat)
        => new(mesh, mat, Matrix4x4.CreateTranslation(position)); 

    public Model3D Eval(Model3D m, EvalContext eval)
    {
        var mat = m.Materials.FirstOrDefault() ?? Material.Default;
        var instancedMesh = PlatonicSolids.TriangulatedCube.Scale(Scale);
        var mergedMesh = m.ToMesh();
        var points = AtFaceCenters ? mergedMesh.Triangles.Map(f => f.Center) : mergedMesh.Points;
        return Model3D.Create(points.Select(p => ToElement(instancedMesh, p, mat)));
    }
}