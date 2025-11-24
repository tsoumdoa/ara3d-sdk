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

    public static void AddMeshes(this RenderScene scene, IReadOnlyList<TriangleMesh3D> meshes)
    {
        var slices = new MeshSliceStruct[scene.Meshes.Count + meshes.Count];

        var numVertices = 0;
        var numIndices = 0;
        foreach (var mesh in meshes)
        {
            numVertices += mesh.Points.Count;
            numIndices += mesh.FaceIndices.Count * 3;
        }

        var vertexBufferOffset = scene.Vertices.Count * 3;
        var vertexBufferSize = numVertices * 3 + vertexBufferOffset;
        var newVertexBuffer = new float[vertexBufferSize];

        // TODO: can be optimized
        scene.Vertices.CopyTo(newVertexBuffer);

        var indexBufferOffset = scene.Indices.Count;
        var indexBufferSize = numIndices + indexBufferOffset;
        var newIndexBuffer = new uint[indexBufferSize];

        // TODO: can be optimized
        scene.Indices.CopyTo(newIndexBuffer);

        var sliceOffset = scene.Meshes.Count;
        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];

            slices[i + sliceOffset].BaseVertex = vertexBufferOffset;
            slices[i + sliceOffset].FirstIndex = (uint)indexBufferOffset;
            slices[i + sliceOffset].IndexCount = (uint)mesh.FaceIndices.Count * 3;

            indexBufferOffset += mesh.FaceIndices.Count * 3;
            vertexBufferOffset += mesh.Points.Count;

            // TODO: can be optimized
            foreach (var pt in mesh.Points)
            {
                newVertexBuffer[vertexBufferOffset++] = pt.X;
                newVertexBuffer[vertexBufferOffset++] = pt.Y;
                newVertexBuffer[vertexBufferOffset++] = pt.Z;
            }

            // TODO: can be optimized
            foreach (var tri in mesh.FaceIndices)
            {
                newIndexBuffer[indexBufferOffset++] = (uint)tri.A.Value;
                newIndexBuffer[indexBufferOffset++] = (uint)tri.B.Value;
                newIndexBuffer[indexBufferOffset++] = (uint)tri.C.Value;
            }
        }
    }
}