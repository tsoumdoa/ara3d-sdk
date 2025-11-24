using Ara3D.DataTable;
using Ara3D.Geometry;

namespace Ara3D.Models;

public class Model3DBuilder 
{
    public List<TriangleMesh3D> Meshes { get; } = [];
    public List<InstanceStruct> Instances { get; } = [];
    
    public Model3D Build()
        => new(Meshes, Instances);

    public void AddInstance(int meshIndex, Matrix4x4 matrix)
        => AddInstance(meshIndex, matrix, Material.Default);

    public void AddInstance(int meshIndex, Material material)
        => AddInstance(meshIndex, Matrix4x4.Identity, material);

    public void AddInstance(int meshIndex, Matrix4x4 matrix, Material material)
        => Instances.Add(new InstanceStruct(matrix, meshIndex, material));

    public void AddInstance(int meshIndex, Material material, Matrix4x4 matrix)
        => AddInstance(meshIndex, matrix, material);
    
    public void AddModel(Model3D model)
    {
        var meshOffset = Meshes.Count;
        Meshes.AddRange(model.Meshes);
        foreach (var inst in model.Instances)
            Instances.Add(inst.WithMeshIndex(inst.MeshIndex + meshOffset));
    }

    public void AddInstance(TriangleMesh3D mesh, Material material, Matrix4x4 matrix)
        => AddInstance(AddMesh(mesh), material, matrix);

    public void AddInstance(TriangleMesh3D mesh, Material material)
        => AddInstance(mesh, material, Matrix4x4.Identity);

    public void AddInstance(TriangleMesh3D mesh, Matrix4x4 matrix)
        => AddInstance(mesh, Material.Default, matrix);

    public void AddInstance(TriangleMesh3D mesh)
        => AddInstance(mesh, Material.Default, Matrix4x4.Identity);

    public int AddMesh(TriangleMesh3D mesh)
    {
        var r = Meshes.Count;
        Meshes.Add(mesh);
        return r;
    }
}