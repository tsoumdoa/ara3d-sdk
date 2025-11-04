using System.Diagnostics;
using Ara3D.Collections;
using Ara3D.DataTable;
using Ara3D.Geometry;

namespace Ara3D.Models;

/// <summary>
/// A model is a collection of elements, meshes, transforms, materials, and meta-data.
/// Elements are the parts of a model. They may share references to meshes, materials, and transforms.
/// If multiple elements share a reference to a transform, then they are intended to move together.
/// </summary>
public class Model3D : ITransformable3D<Model3D>
{
    public Model3D(
        IReadOnlyList<TriangleMesh3D> meshes,
        IReadOnlyList<InstanceStruct> instances,
        IDataSet? dataSet = null)
    {
        Meshes = meshes;
        Instances = instances;
        DataSet = dataSet ?? new ReadOnlyDataSet([]);
    }

    public IReadOnlyList<TriangleMesh3D> Meshes { get; }
    public IReadOnlyList<InstanceStruct> Instances { get; }
    public IDataSet DataSet { get; }

    public Model3D WithMeshes(IReadOnlyList<TriangleMesh3D> meshes)
        => new(meshes, Instances, DataSet);

    public Model3D WithInstances(IReadOnlyList<InstanceStruct> instances)
        => new(Meshes, instances, DataSet);

    public Model3D WithDataSet(IDataSet dataSet)
        => new(Meshes, Instances, dataSet);

    public Model3D Transform(Transform3D transform)
        => WithInstances(Instances.Select(i => i.Transform(transform)));

    public static Model3D Create(TriangleMesh3D mesh, Material material, Matrix4x4 matrix)
        => new([mesh], [new(matrix, 0, material)]);

    public static Model3D Create(TriangleMesh3D mesh, Material material, IReadOnlyList<Matrix4x4> matrices)
        => new([mesh], matrices.Select(m => new InstanceStruct(m, 0, material)));

    public static Model3D Create(TriangleMesh3D mesh, Material material)
        => Create(mesh, material, Matrix4x4.Identity);

    public static Model3D Create(TriangleMesh3D mesh)
        => Create(mesh, Material.Default);

    public static implicit operator Model3D(TriangleMesh3D m)
        => Create(m);
}