using Ara3D.Geometry;

namespace Ara3D.Models;

public static class Model3DOps
{
    public static Model3D Clone(this Model3D model, IReadOnlyList<Vector3> positions)
        => model.Clone(positions.Map(Matrix4x4.CreateTranslation));

    public static Model3D Clone(this Model3D model, IReadOnlyList<Matrix4x4> matrices)
    {
        var newMatrices = new List<Matrix4x4>();
        foreach (var om in matrices)
        {
            foreach (var tm in model.Transforms)
            {
                newMatrices.Add(tm * om);
            }
        }

        var newElementStructs = new List<ElementStruct>();
        var cnt = matrices.Count;
        var transformCount = model.Transforms.Count;

        for (var i = 0; i < cnt; i++)
        {
            foreach (var es in model.ElementStructs)
            {
                var newTransformIndex = es.TransformIndex + transformCount * i;
                var es2 = new ElementStruct(es.EntityIndex, es.MaterialIndex, es.MeshIndex, newTransformIndex);
                newElementStructs.Add(es2);
            }
        }

        return model.WithTransforms(newMatrices).WithStructs(newElementStructs);
    }
}