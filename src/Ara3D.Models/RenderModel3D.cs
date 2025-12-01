using System.Runtime.CompilerServices;
using Ara3D.Geometry;
using Ara3D.Memory;

namespace Ara3D.Models;

/// <summary>
/// This is exactly what the GPU expects for rendering. 
/// </summary>
public class RenderModel3D
(
    IMemoryOwner<float> vertices,
    IMemoryOwner<uint> indices,
    IMemoryOwner<MeshSliceStruct> meshSlices,
    IMemoryOwner<InstanceStruct> instances,
    IMemoryOwner<InstanceGroupStruct> groups
)
    : IModel3D
{
    public IMemoryOwner<float> Vertices { get; set; } = vertices;
    public IMemoryOwner<uint> Indices { get; set; } = indices;
    public IMemoryOwner<MeshSliceStruct> MeshSlices { get; set; } = meshSlices;
    public IMemoryOwner<InstanceStruct> Instances { get; set; } = instances;
    public IMemoryOwner<InstanceGroupStruct> InstanceGroups { get; set; } = groups;

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
        var points = BufferExtensions.Cast<Point3D>(Vertices);
        var faces = BufferExtensions.Cast<Integer3>(Indices);
        var pointSlice = points.Slice(slice.BaseVertex, slice.VertexCount);
        var faceSlice = faces.Slice(slice.FirstIndex * 3, slice.IndexCount / 3);
        return new TriangleMesh3D(pointSlice, faceSlice);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
        MeshSlices.Dispose();
        Instances.Dispose();
        InstanceGroups.Dispose();
    }

    public IModel3D Transform(Transform3D t)
        => Model3DExtensions.Transform(this, t);
}