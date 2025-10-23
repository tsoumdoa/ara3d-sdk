using Ara3D.Memory;

namespace Ara3D.Studio.Data
{
    public interface IRenderScene
    {
        IBuffer<uint> Indices { get; }
        IBuffer<float> Vertices { get; }
        IBuffer<MeshSliceStruct> Meshes { get; }
        IBuffer<InstanceStruct> Instances { get; }
        IBuffer<InstanceGroupStruct> InstanceGroups { get; }
    }
}