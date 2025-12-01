using Ara3D.Geometry;

namespace Ara3D.Models;

public interface IModel3D : ITransformable3D<IModel3D>
{
    IReadOnlyList<TriangleMesh3D> Meshes { get; }
    IReadOnlyList<InstanceStruct> Instances { get; }
}