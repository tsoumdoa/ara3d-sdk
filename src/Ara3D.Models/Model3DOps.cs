using Ara3D.Geometry;

namespace Ara3D.Models;

public interface IModel3D { }

public static class Model3DOps
{
    public enum ObjectId { }

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


}