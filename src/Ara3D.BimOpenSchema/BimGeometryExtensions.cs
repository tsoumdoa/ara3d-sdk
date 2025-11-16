using System.Collections.Generic;
using System.Linq;
using Ara3D.Collections;
using Ara3D.DataTable;
using Ara3D.Geometry;
using Ara3D.Models;
using Ara3D.Utils;

namespace Ara3D.BimOpenSchema;

public static class BimGeometryExtensions
{
    public static int GetNumMaterials(this BimGeometry self) => self.MaterialRed.Length;
    public static int GetNumVertices(this BimGeometry self) => self.VertexX.Length;
    public static int GetNumFaces(this BimGeometry self) => self.GetNumIndices() / 3;
    public static int GetNumIndices(this BimGeometry self) => self.IndexBuffer.Length;
    public static int GetNumTransforms(this BimGeometry self) => self.TransformTX.Length;
    public static int GetNumMeshes(this BimGeometry self) => self.MeshIndexOffset.Length;
    public static int GetNumElements(this BimGeometry self) => self.ElementMeshIndex.Length;

    public static IReadOnlyList<Point3D> GetMeshPoints(this BimGeometry self, int meshIndex)
    {
        var r = new List<Point3D>();

        var vertexOffset = self.MeshVertexOffset[meshIndex];
        var nextVertexOffset = meshIndex < self.GetNumMeshes() - 1
            ? self.MeshVertexOffset[meshIndex + 1]
            : self.VertexX.Length;
        var vertexCount = nextVertexOffset - vertexOffset;

        for (var i = 0; i < vertexCount; i++)
        {
            var x = self.VertexX[i + vertexOffset];
            var y = self.VertexY[i + vertexOffset];
            var z = self.VertexZ[i + vertexOffset];
            r.Add((x, y, z));
        }

        return r;
    }

    public static IReadOnlyList<Integer3> GetMeshFaceIndices(this BimGeometry self, int meshIndex)
    {
        var r = new List<Integer3>();

        var indexOffset = self.MeshIndexOffset[meshIndex];
        
        var nextIndexOffset = meshIndex < self.GetNumMeshes() - 1
            ? self.MeshIndexOffset[meshIndex + 1]
            : self.IndexBuffer.Length;

        var indexCount = nextIndexOffset - indexOffset;

        for (var i = 0; i < indexCount; i += 3)
        {
            var a = self.IndexBuffer[i + indexOffset + 0];
            var b = self.IndexBuffer[i + indexOffset + 1];
            var c = self.IndexBuffer[i + indexOffset + 2];
            r.Add((a, b, c));
        }

        return r;
    }

    public static TriangleMesh3D GetMesh(this BimGeometry self, int meshIndex)
        => (self.GetMeshPoints(meshIndex), self.GetMeshFaceIndices(meshIndex));

    public static Color GetColor(this BimGeometry self, int materialIndex)
        => new(
            self.MaterialRed[materialIndex].ToNormalizedFloat(), 
            self.MaterialGreen[materialIndex].ToNormalizedFloat(), 
            self.MaterialBlue[materialIndex].ToNormalizedFloat(), 
            self.MaterialAlpha[materialIndex].ToNormalizedFloat());

    public static Material GetMaterial(this BimGeometry self, int materialIndex, Material defaultMaterial)
        => materialIndex < 0 ? defaultMaterial : 
            new(self.GetColor(materialIndex), 
            self.MaterialMetallic[materialIndex].ToNormalizedFloat(),
            self.MaterialRoughness[materialIndex].ToNormalizedFloat());

    public static InstanceStruct GetInstanceStruct(this BimGeometry self, int elementIndex)
        => new(self.GetElementMatrix(elementIndex), self.ElementMeshIndex[elementIndex], self.GetElementMaterial(elementIndex));

    public static Material GetMaterial(this BimGeometry self, int materialIndex)
        => new(self.GetColor(materialIndex), 
            self.MaterialMetallic[materialIndex].ToNormalizedFloat(),
            self.MaterialRoughness[materialIndex].ToNormalizedFloat());

    public static Material GetElementMaterial(this BimGeometry self, int elementIndex)
        => self.GetMaterial(self.ElementMaterialIndex[elementIndex]);

    public static Matrix4x4 GetElementMatrix(this BimGeometry self, int elementIndex)
        => new(self.GetTransformMatrix(self.ElementTransformIndex[elementIndex]));

    public static Matrix4x4 GetTranslationMatrix(this BimGeometry self, int i)
        => Matrix4x4.CreateTranslation(self.GetTranslation(i));

    public static Vector3 GetTranslation(this BimGeometry self, int i)
        => new(self.TransformTX[i], self.TransformTY[i], self.TransformTZ[i]);

    public static Vector3 GetScale(this BimGeometry self, int i)
        => new(self.TransformSX[i], self.TransformSY[i], self.TransformSZ[i]);

    public static Matrix4x4 GetScaleMatrix(this BimGeometry self, int i)
        => Matrix4x4.CreateScale(self.TransformSX[i], self.TransformSY[i], self.TransformSZ[i]);

    public static Quaternion GetRotation(this BimGeometry self, int i)
        => new(self.TransformQX[i], self.TransformQY[i], self.TransformQZ[i], self.TransformQW[i]);

    public static Matrix4x4 GetRotationMatrix(this BimGeometry self, int i)
        => Matrix4x4.CreateFromQuaternion(self.GetRotation(i));

    public static Matrix4x4 GetTransformMatrix(this BimGeometry self, int i)
        => self.GetTranslationMatrix(i) * self.GetRotationMatrix(i) * self.GetScaleMatrix(i);

    public static Model3D ToModel3D(this BimGeometry self)
    {
        var meshes = self.GetNumMeshes().MapRange(i => self.GetMesh(i)).ToList();
        var instances = self.GetNumElements().MapRange(i => self.GetInstanceStruct(i)).ToList();
        return new Model3D(meshes, instances);
    }

    public static DataTableBuilder AddColumn<T>(this DataTableBuilder self, BimGeometry model, T[] data, string name)
    {
        self.AddColumn(data, name);
        return self;
    }

    public static IDataSet ToDataSet(this BimGeometry self)
    {
        var r = new DataSetBuilder();
        r.AddTable("Material")
            .AddColumn(self, self.MaterialRed, nameof(self.MaterialRed))
            .AddColumn(self, self.MaterialGreen, nameof(self.MaterialGreen))
            .AddColumn(self, self.MaterialBlue, nameof(self.MaterialBlue))
            .AddColumn(self, self.MaterialAlpha, nameof(self.MaterialAlpha))
            .AddColumn(self, self.MaterialMetallic, nameof(self.MaterialMetallic))
            .AddColumn(self, self.MaterialRoughness, nameof(self.MaterialRoughness));

        r.AddTable("Transform")
            .AddColumn(self, self.TransformTX, nameof(self.TransformTX))
            .AddColumn(self, self.TransformTY, nameof(self.TransformTY))
            .AddColumn(self, self.TransformTZ, nameof(self.TransformTZ))
            .AddColumn(self, self.TransformQX, nameof(self.TransformQX))
            .AddColumn(self, self.TransformQY, nameof(self.TransformQY))
            .AddColumn(self, self.TransformQZ, nameof(self.TransformQZ))
            .AddColumn(self, self.TransformQW, nameof(self.TransformQW))
            .AddColumn(self, self.TransformSX, nameof(self.TransformSX))
            .AddColumn(self, self.TransformSX, nameof(self.TransformSY))
            .AddColumn(self, self.TransformSX, nameof(self.TransformSZ));

        r.AddTable("Vertex")
            .AddColumn(self, self.VertexX, nameof(self.VertexX))
            .AddColumn(self, self.VertexY, nameof(self.VertexY))
            .AddColumn(self, self.VertexZ, nameof(self.VertexZ));

        r.AddTable("Index")
            .AddColumn(self, self.IndexBuffer, nameof(self.IndexBuffer));

        r.AddTable("Element")
            .AddColumn(self, self.ElementEntityIndex, nameof(self.ElementEntityIndex))
            .AddColumn(self, self.ElementMaterialIndex, nameof(self.ElementMaterialIndex))
            .AddColumn(self, self.ElementMeshIndex, nameof(self.ElementMeshIndex))
            .AddColumn(self, self.ElementTransformIndex, nameof(self.ElementTransformIndex));

        r.AddTable("Mesh")
            .AddColumn(self, self.MeshIndexOffset, nameof(self.MeshIndexOffset))
            .AddColumn(self, self.MeshVertexOffset, nameof(self.MeshVertexOffset));

        return r;
    }

    
    public static BimGeometry ToBimGeometry(this Model3D self)
    {
        var bgb = new BimGeometryBuilder();
        bgb.AddMeshes(self.Meshes);

        foreach (var inst in self.Instances)
        {
            var materialIndex = bgb.AddMaterial(inst.Material);
            var transformIndex = bgb.AddTransform(inst.Matrix4x4);
            bgb.AddElement(inst.EntityIndex, materialIndex, inst.MeshIndex, transformIndex);
        }

        return bgb.BuildModel();
    }

    public static T[] ReadColumn<T>(this IDataSet set, string tableName, string columnName)
        => set.GetTable(tableName).GetColumn(columnName).GetTypedValues<T>();

    public static BimGeometry ToBimGeometry(this IDataSet self)
    {
        var r = new BimGeometry();
        r.MaterialRed = ReadColumn<byte>(self, "Material", nameof(r.MaterialRed));
        r.MaterialGreen = ReadColumn<byte>(self, "Material", nameof(r.MaterialGreen));
        r.MaterialBlue = ReadColumn<byte>(self, "Material", nameof(r.MaterialBlue));
        r.MaterialAlpha = ReadColumn<byte>(self, "Material", nameof(r.MaterialAlpha));
        r.MaterialMetallic = ReadColumn<byte>(self, "Material", nameof(r.MaterialMetallic));
        r.MaterialRoughness = ReadColumn<byte>(self, "Material", nameof(r.MaterialRoughness));

        r.TransformTX = ReadColumn<float>(self, "Transform", nameof(r.TransformTX));
        r.TransformTY = ReadColumn<float>(self, "Transform", nameof(r.TransformTY));
        r.TransformTZ = ReadColumn<float>(self, "Transform", nameof(r.TransformTZ));
        r.TransformQX = ReadColumn<float>(self, "Transform", nameof(r.TransformQX));
        r.TransformQY = ReadColumn<float>(self, "Transform", nameof(r.TransformQY));
        r.TransformQZ = ReadColumn<float>(self, "Transform", nameof(r.TransformQZ));
        r.TransformQW = ReadColumn<float>(self, "Transform", nameof(r.TransformQW));
        r.TransformSX = ReadColumn<float>(self, "Transform", nameof(r.TransformSX));
        r.TransformSY = ReadColumn<float>(self, "Transform", nameof(r.TransformSY));
        r.TransformSZ = ReadColumn<float>(self, "Transform", nameof(r.TransformSZ));

        r.VertexX = ReadColumn<float>(self, "Vertex", nameof(r.VertexX));
        r.VertexY = ReadColumn<float>(self, "Vertex", nameof(r.VertexY));
        r.VertexZ = ReadColumn<float>(self, "Vertex", nameof(r.VertexZ));

        r.IndexBuffer = ReadColumn<int>(self, "Index", nameof(r.IndexBuffer));

        r.ElementMaterialIndex = ReadColumn<int>(self, "Element", nameof(r.ElementMaterialIndex));
        r.ElementMeshIndex = ReadColumn<int>(self, "Element", nameof(r.ElementMeshIndex));
        r.ElementTransformIndex = ReadColumn<int>(self, "Element", nameof(r.ElementTransformIndex));

        r.MeshIndexOffset = ReadColumn<int>(self, "Mesh", nameof(r.MeshIndexOffset));
        r.MeshVertexOffset = ReadColumn<int>(self, "Mesh", nameof(r.MeshVertexOffset));

        return r;
    }
}