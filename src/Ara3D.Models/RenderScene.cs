using Ara3D.Memory;

namespace Ara3D.Models;

public class RenderScene(
    IBuffer<float> vertices,
    IBuffer<uint> indices,
    IBuffer<MeshSliceStruct> meshes,
    IBuffer<InstanceStruct> instances,
    IBuffer<InstanceGroupStruct> groups)
    : IRenderScene
{
    public IBuffer<float> Vertices { get; } = vertices;
    public IBuffer<uint> Indices { get; } = indices;
    public IBuffer<MeshSliceStruct> Meshes { get; } = meshes;
    public IBuffer<InstanceStruct> Instances { get; } = instances;
    public IBuffer<InstanceGroupStruct> InstanceGroups { get; } = groups;
}