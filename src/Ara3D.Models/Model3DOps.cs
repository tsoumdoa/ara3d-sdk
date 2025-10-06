using Ara3D.Geometry;

namespace Ara3D.Models;

public interface IModel3D { }

public static class Model3DOps
{
    public enum ObjectId { }

    /*
    public static IModel3D Select(this IModel3D model, Func<ObjectId, bool> f)
        => throw new NotImplementedException();

    public static IModel3D UpdateTransform(this IModel3D model, Func<ObjectId, Matrix4x4> f)
        => throw new NotImplementedException();

    public static IModel3D UpdateMaterial(this IModel3D model, Func<ObjectId, Material> f)
        => throw new NotImplementedException();

    public static IModel3D AddMeshes(this IModel3D model, IReadOnlyList<TriangleMesh3D> meshes)
        => throw new NotImplementedException();

    public static IModel3D StoreSelection(this IModel3D model)
        => throw new NotImplementedException();

    public static IModel3D RestoreSelection(this IModel3D model)
        => throw new NotImplementedException();

    public static IModel3D SelectAll(this IModel3D model)
        => throw new NotImplementedException();

    */

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