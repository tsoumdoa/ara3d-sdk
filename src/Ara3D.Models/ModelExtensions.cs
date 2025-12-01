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

    public static RenderModel3D TransformInstances(this RenderModel3D renderModel3D, Func<InstanceStruct, InstanceStruct> f)
    {
        renderModel3D.Instances.Transform(f);
        return renderModel3D;
    }

    public static RenderModel3D TransformMaterials(this RenderModel3D renderModel3D, Func<Material, Material> f)
        => renderModel3D.TransformInstances(inst => inst.TransformMaterial(f));

    public static RenderModel3D TransformVertices(this RenderModel3D renderModel3D, Func<Point3D, Point3D> f)
    {
        renderModel3D.Vertices.CastMemory<Point3D>().Transform(f);
        return renderModel3D;
    }

    public static RenderModel3D TransformVertices(this RenderModel3D renderModel3D, Func<Vector3, Vector3> f)
    {
        renderModel3D.Vertices.CastMemory<Vector3>().Transform(f);
        return renderModel3D;
    }
}