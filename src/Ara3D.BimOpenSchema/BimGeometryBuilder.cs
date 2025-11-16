using System.Collections.Generic;
using System.Linq;
using Ara3D.Geometry;
using Ara3D.Models;
using Ara3D.Utils;

namespace Ara3D.BimOpenSchema;

public record ElementStruct(
    int EntityIndex, 
    int MaterialIndex,
    int MeshIndex,
    int TransformIndex);

/// <summary>
/// This class is provided to make it easier to build a BIM geometry object incrementally. 
/// </summary>
public class BimGeometryBuilder
{
    public List<ElementStruct> Elements { get; private set; } = new();
    public List<TriangleMesh3D> Meshes { get; private set; } = new();
    public IndexedSet<Material> Materials { get; private set; } = new();
    public IndexedSet<Matrix4x4> Matrices { get; private set; } = new();

    public void AddMeshes(IEnumerable<TriangleMesh3D> meshes)
        => Meshes.AddRange(meshes);

    public int AddMesh(TriangleMesh3D mesh)
    {
        Meshes.Add(mesh);
        return Meshes.Count - 1;
    }

    public int AddMaterial(Material material)
        => Materials.Add(material);

    public int AddElement(int entityIndex, int materialIndex, int meshIndex, int transformIndex)
    {
        var es = new ElementStruct(entityIndex, materialIndex, meshIndex, transformIndex);
        Elements.Add(es);
        return Elements.Count - 1;
    }

    public int AddTransform(Matrix4x4 matrix)
        => Matrices.Add(matrix);

    public BimGeometry BuildModel()
    {
        var r = new BimGeometry();

        r.ElementEntityIndex = new int[Elements.Count];
        r.ElementMaterialIndex = new int[Elements.Count];
        r.ElementMeshIndex = new int[Elements.Count];
        r.ElementTransformIndex = new int[Elements.Count];

        for (var i = 0; i < Elements.Count; i++)
        {
            var e = Elements[i];
            r.ElementEntityIndex[i] = e.EntityIndex;
            r.ElementMaterialIndex[i] = e.MaterialIndex;
            r.ElementMeshIndex[i] = e.MeshIndex;
            r.ElementTransformIndex[i] = e.TransformIndex;
        }
        
        var vertX = new List<float>();
        var vertY = new List<float>();
        var vertZ = new List<float>();
        var indices = new List<int>();

        r.MeshVertexOffset = new int[Meshes.Count];
        r.MeshIndexOffset = new int[Meshes.Count];

        for (var i=0; i < Meshes.Count; i++)
        {
            var m = Meshes[i];
            r.MeshVertexOffset[i] = vertX.Count;
            r.MeshIndexOffset[i] = indices.Count;
            foreach (var vert in m.Points)
            {
                vertX.Add(vert.X);
                vertY.Add(vert.Y);
                vertZ.Add(vert.Z);
            }

            foreach (var face in m.FaceIndices)
            {
                indices.Add(face.A);
                indices.Add(face.B);
                indices.Add(face.C);
            }
        }
        
        r.VertexX = vertX.ToArray();
        r.VertexY = vertY.ToArray();
        r.VertexZ = vertZ.ToArray();
        r.IndexBuffer = indices.ToArray();

        var materials = Materials.OrderedMembers().ToList();

        r.MaterialRed = new byte[materials.Count];
        r.MaterialGreen = new byte[materials.Count];
        r.MaterialBlue = new byte[materials.Count];
        r.MaterialAlpha = new byte[materials.Count];
        r.MaterialRoughness = new byte[materials.Count];
        r.MaterialMetallic = new byte[materials.Count];

        for (var i=0; i < materials.Count; i++)
        {
            var m = materials[i];
            r.MaterialRed[i] = m.Color.R.Value.ToByteFromNormalized();
            r.MaterialGreen[i] = m.Color.G.Value.ToByteFromNormalized();
            r.MaterialBlue[i] = m.Color.B.Value.ToByteFromNormalized();
            r.MaterialAlpha[i] = m.Color.A.Value.ToByteFromNormalized();
            r.MaterialRoughness[i] = m.Roughness.ToByteFromNormalized();
            r.MaterialMetallic[i] = m.Metallic.ToByteFromNormalized();
        }

        var transforms = Matrices.OrderedMembers().ToList();
        var n = transforms.Count;
        r.TransformQW = new float[n];
        r.TransformQX = new float[n];
        r.TransformQY = new float[n];
        r.TransformQZ = new float[n];
        r.TransformSX = new float[n];
        r.TransformSY = new float[n];
        r.TransformSZ = new float[n];
        r.TransformTX = new float[n];
        r.TransformTY = new float[n];
        r.TransformTZ = new float[n];
        
        for (var i=0; i < n; i++)
        {
            var mat = transforms[i];
            if (!System.Numerics.Matrix4x4.Decompose(mat, out var scale, out var rot, out var tr))
            {
                scale = Vector3.One;
                rot = Quaternion.Identity;
                tr = Vector3.Zero;
            }

            r.TransformQW[i] = rot.W;
            r.TransformQX[i] = rot.X;
            r.TransformQY[i] = rot.Y;
            r.TransformQZ[i] = rot.Z;
            r.TransformSX[i] = scale.X;
            r.TransformSY[i] = scale.Y;
            r.TransformSZ[i] = scale.Z;
            r.TransformTX[i] = tr.X;
            r.TransformTY[i] = tr.Y;
            r.TransformTZ[i] = tr.Z;
        }
        return r;
    }
}