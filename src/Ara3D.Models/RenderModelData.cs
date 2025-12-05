using System.Runtime.CompilerServices;
using Ara3D.Geometry;
using Ara3D.Memory;

namespace Ara3D.Models;

/// <summary>
/// This is a long-lived data structure that contains the data used for rendering.
/// </summary>
public class RenderModelData
    : IModel3D
{
    public UnmanagedList<Point3D> Vertices { get; private set; } = new();
    public UnmanagedList<Integer3> FaceIndices { get; private set; } = new();
    public UnmanagedList<MeshSliceStruct> MeshSlices { get; private set; } = new();
    public UnmanagedList<InstanceStruct> Instances { get; private set; } = new();

    public IReadOnlyList<TriangleMesh3D> Meshes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MeshSlices.Map(ToMesh); 
    }

    IReadOnlyList<InstanceStruct> IModel3D.Instances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instances;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TriangleMesh3D ToMesh(MeshSliceStruct slice)
    {
        var pointSlice = Vertices.Slice(slice.BaseVertex, slice.VertexCount);
        var faceSlice = FaceIndices.Slice(slice.FirstIndex * 3, slice.IndexCount / 3);
        return new TriangleMesh3D(pointSlice, faceSlice);
    }

    public void Dispose()
    {
        Vertices?.Dispose();
        FaceIndices?.Dispose();
        MeshSlices?.Dispose();
        Instances?.Dispose();
        Vertices = null;
        FaceIndices = null;
        MeshSlices = null;
        Instances = null;
    }

    public IModel3D Transform(Transform3D t)
        => Model3DExtensions.Transform(this, t);

    public void Update(IBuffer<Point3D> vertices, IBuffer<Integer3> indices, IBuffer<MeshSliceStruct> meshSlices,
        IBuffer<InstanceStruct> instances)
    {
        Vertices.CopyFrom(vertices);
        FaceIndices.CopyFrom(indices);
        MeshSlices.CopyFrom(meshSlices);
        Instances.CopyFrom(instances);
    }

    public void Update(IModel3D model)
    {
        Vertices.Clear();
        FaceIndices.Clear();
        MeshSlices.Clear();
        Instances.Clear();

        foreach (var mesh in model.Meshes)
        {
            var faceIndices = mesh.FaceIndices;
            var points = mesh.Points;

            var meshSlice = new MeshSliceStruct()
            {
                FirstIndex = (uint)FaceIndices.Count * 3,
                IndexCount = (uint)faceIndices.Count * 3,
                BaseVertex = (int)Vertices.Count,
                VertexCount = points.Count
            };

            MeshSlices.Add(meshSlice);
            Vertices.AddRange(points);
            FaceIndices.AddRange(faceIndices);
        }

        Instances.AddRange(model.Instances);
    }
}