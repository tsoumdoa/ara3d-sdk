using Ara3D.Geometry;

namespace Ara3D.Models;

public interface IModel3D : IDisposable
{
    IReadOnlyList<TriangleMesh3D> Meshes { get; }
    IReadOnlyList<InstanceStruct> Instances { get; }
    void UpdateScene(RenderScene scene);
}