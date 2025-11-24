using Ara3D.Memory;

namespace Ara3D.Models;

public class RenderScene
(
    IMemoryOwner<float> vertices,
    IMemoryOwner<uint> indices,
    IMemoryOwner<MeshSliceStruct> meshes,
    IMemoryOwner<InstanceStruct> instances,
    IMemoryOwner<InstanceGroupStruct> groups
)
    : IDisposable
{
    public IMemoryOwner<float> Vertices { get; set; } = vertices;
    public IMemoryOwner<uint> Indices { get; set; } = indices;
    public IMemoryOwner<MeshSliceStruct> Meshes { get; set; } = meshes;
    public IMemoryOwner<InstanceStruct> Instances { get; set; } = instances;
    public IMemoryOwner<InstanceGroupStruct> InstanceGroups { get; set; } = groups;

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
        Meshes.Dispose();
        Instances.Dispose();
        InstanceGroups.Dispose();
    }
}