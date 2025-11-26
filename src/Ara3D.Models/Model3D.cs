using System.Diagnostics;
using Ara3D.Collections;
using Ara3D.Geometry;
using Ara3D.Utils;

namespace Ara3D.Models;

/// <summary>
/// A model is a collection of meshes and instances.
/// A mesh is a triangular mesh with vertices and indices. 
/// Instances are: transform, mesh index, material data, and an entity index. 
/// </summary>
public class Model3D 
    : ITransformable3D<Model3D>, IModel3D
{
    public Model3D(
        IReadOnlyList<TriangleMesh3D> meshes,
        IReadOnlyList<InstanceStruct> instances)
    {
        Meshes = meshes;
        Instances = instances;
    }

    public IReadOnlyList<TriangleMesh3D> Meshes { get; }
    public IReadOnlyList<InstanceStruct> Instances { get; }

    public Model3D WithMeshes(IReadOnlyList<TriangleMesh3D> meshes)
        => new(meshes, Instances);

    public Model3D WithInstances(IReadOnlyList<InstanceStruct> instances)
        => new(Meshes, instances);

    public Model3D Transform(Transform3D transform)
        => WithInstances(Instances.Select(i => i.Transform(transform)));

    public static Model3D Create(TriangleMesh3D mesh, Material material, Matrix4x4 matrix)
        => new([mesh], [new(-1, matrix, 0, material)]);

    public static Model3D Create(TriangleMesh3D mesh, Material material, IReadOnlyList<Matrix4x4> matrices)
        => new([mesh], matrices.Select(m => new InstanceStruct(-1, m, 0, material)));

    public static Model3D Create(TriangleMesh3D mesh, Material material)
        => Create(mesh, material, Matrix4x4.Identity);

    public static Model3D Create(TriangleMesh3D mesh)
        => Create(mesh, Material.Default);

    public static implicit operator Model3D(TriangleMesh3D m)
        => Create(m);

    public void UpdateScene(RenderScene scene)
    {
        // TODO: 
        // When this works, I should probably remove "RenderSceneBuilder"
        // OR ... RenderSceneBuilder would replace this. 
        throw new NotImplementedException();
    }

    public Model3D FilterAndRemoveUnusedMeshes(Func<InstanceStruct, bool> f)
        => new Model3D(Meshes, Instances.Where(f).ToList()).RemoveUnusedMeshes();

    public Model3D RemoveUnusedMeshes()
    {
        var newMeshIndices = new IndexedSet<int>();
        var newInstances = new List<InstanceStruct>();
        var newMeshes = new List<TriangleMesh3D>();
        foreach (var inst in Instances)
        {
            if (inst.MeshIndex < 0)
            {
                newInstances.Add(inst);
                continue;
            }

            if (!newMeshIndices.Contains(inst.MeshIndex))
            {
                var mesh = Meshes[inst.MeshIndex];
                var newMeshIndex = newMeshIndices.Add(inst.MeshIndex);
                newMeshes.Add(mesh);
                Debug.Assert(newMeshIndex == newMeshIndices.Count - 1);
                newInstances.Add(inst.WithMeshIndex(newMeshIndex));
            }
            else
            {
                var newMeshIndex = newMeshIndices[inst.MeshIndex];
                newInstances.Add(inst.WithMeshIndex(newMeshIndex));
            }
        }

        return new(newMeshes, newInstances);
    }

    public void Dispose()
    { }
}