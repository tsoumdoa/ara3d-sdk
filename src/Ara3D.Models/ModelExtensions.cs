using Ara3D.Collections;
using Ara3D.Geometry;
using Ara3D.Memory;

namespace Ara3D.Models;

public static class ModelExtensions
{
    public static void Transform<T>(this IBuffer<T> self, Func<T, T> f)
    {
        for (var i = 0; i < self.Count; i++)
            self[i] = f(self[i]);
    }

    public static InstanceStruct TransformMaterial(this InstanceStruct self, Func<Material, Material> f)
        => self.WithMaterial(f(self.Material));

    public static RenderScene TransformInstances(this RenderScene scene, Func<InstanceStruct, InstanceStruct> f)
    {
        scene.Instances.Transform(f);
        return scene;
    }

    public static RenderScene TransformMaterials(this RenderScene scene, Func<Material, Material> f)
        => scene.TransformInstances(inst => inst.TransformMaterial(f));

    public static RenderScene TransformVertices(this RenderScene scene, Func<Point3D, Point3D> f)
    {
        scene.Vertices.CastMemory<Point3D>().Transform(f);
        return scene;
    }

    public static RenderScene TransformVertices(this RenderScene scene, Func<Vector3, Vector3> f)
    {
        scene.Vertices.CastMemory<Vector3>().Transform(f);
        return scene;
    }
}