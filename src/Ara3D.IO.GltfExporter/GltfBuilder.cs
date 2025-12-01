using System.Diagnostics;
using System.Runtime.InteropServices;
using Ara3D.Geometry;
using Ara3D.Models;
using Ara3D.Utils;

namespace Ara3D.IO.GltfExporter;

public class GltfBuilder
{
    public class GltfMeshSlice
    {
        public int VertexOffset;
        public int VertexCount;
        public int FaceOffset;
        public int FaceCount;

        public int VertexByteOffset => VertexOffset * 3 * sizeof(float);
        public int FaceByteOffset => FaceOffset * 3 * sizeof(int);
    }

    public List<Point3D> Vertices { get; } = new();
    public List<Integer3> Faces { get; } = new();
    public GltfData Data = new();

    public const string SCALAR_STR = "SCALAR";
    public const string FACE_STR = "FACE";
    public const string VEC3_STR = "VEC3";
    public const string POSITION_STR = "POSITION";

    public const int VERTEX_VIEW_INDEX = 0;
    public const int FACE_VIEW_INDEX = 1; 
    
    public GltfMaterial ToGltfMaterial(InstanceStruct inst)
        => ToGltfMaterial(inst.Material);

    public GltfMaterial ToGltfMaterial(Material mat)
        => new()
        {
            pbrMetallicRoughness = new GltfPbr()
            {
                baseColorFactor = [mat.Color.R, mat.Color.G, mat.Color.B, mat.Color.A],
                metallicFactor = mat.Metallic,
                roughnessFactor = mat.Roughness
            }
        };

    public GltfAccessor GetVertexAccessor(GltfMeshSlice slice)
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        var maxZ = float.MinValue;
        for (var i = 0; i < slice.VertexCount; i++)
        {
            var v = Vertices[slice.VertexOffset + i];
            minX = Math.Min(minX, v.X);
            minY = Math.Min(minY, v.Y);
            minZ = Math.Min(minZ, v.Z);
            maxX = Math.Max(maxX, v.X);
            maxY = Math.Max(maxY, v.Y);
            maxZ = Math.Max(maxZ, v.Z);
        }

        return new GltfAccessor(VERTEX_VIEW_INDEX, slice.VertexByteOffset, 
            GltfComponentType.FLOAT, slice.VertexCount, VEC3_STR,
            [minX, minY, minZ], [maxX, maxY, maxZ], POSITION_STR);
    }
    
    public GltfAccessor GetIndexAccessor(GltfMeshSlice slice)
    {
        var min = int.MaxValue;
        var max = int.MinValue;        
        for (var i = 0; i < slice.FaceCount; i++)
        {
            var f = Faces[slice.FaceOffset + i];
            for (var j = 0; j < 3; j++)
            {
                min = Math.Min(min, f[j]);
                max = Math.Max(max, f[j]);
            }
        }

        return new GltfAccessor(FACE_VIEW_INDEX, slice.FaceByteOffset, 
            GltfComponentType.UNSIGNED_INT, slice.FaceCount * 3, SCALAR_STR, 
            [min], [max], FACE_STR);
    }

    public GltfMeshSlice CreateMeshSlice(TriangleMesh3D mesh)
    {
        var slice = new GltfMeshSlice
        {
            VertexOffset = Vertices.Count,
            VertexCount = mesh.Points.Count,
            FaceOffset = Faces.Count,
            FaceCount = mesh.FaceIndices.Count
        };
        Vertices.AddRange(mesh.Points);
        Faces.AddRange(mesh.FaceIndices);
        return slice;
    }

    public void SetModel(IModel3D model)
    {
        Debug.Assert(Data.meshes.Count == 0);
        Debug.Assert(Data.accessors.Count == 0);
        Debug.Assert(Data.materials.Count == 0);
        Debug.Assert(Data.accessors.Count == 0);
        Debug.Assert(Data.nodes.Count == 0);

        var mats = model.Instances.Select(i => i.Material).ToIndexedSet();
        Data.materials.AddRange(mats.OrderedMembers().Select(ToGltfMaterial));

        var slices = model.Meshes.Select(CreateMeshSlice).ToList();

        foreach (var slice in slices)
        {
            var vertexAccessor = GetVertexAccessor(slice);
            var indexAccessor = GetIndexAccessor(slice);

            Data.accessors.Add(vertexAccessor);
            Data.accessors.Add(indexAccessor);
        }

        foreach (var instance in model.Instances)
        {
            var matIndex = mats.IndexOf(instance.Material);
            var transform = instance.Matrix4x4;

            var vertexAccessorIndex = instance.MeshIndex * 2 + VERTEX_VIEW_INDEX;
            var indexAccessorIndex = instance.MeshIndex * 2 + FACE_VIEW_INDEX;

            var vertexAccessor = Data.accessors[vertexAccessorIndex];
            var indexAccessor = Data.accessors[indexAccessorIndex];

            Debug.Assert(vertexAccessor.componentType == GltfComponentType.FLOAT);
            Debug.Assert(indexAccessor.componentType == GltfComponentType.UNSIGNED_INT);

            Debug.Assert(vertexAccessor.count == slices[instance.MeshIndex].VertexCount);
            Debug.Assert(indexAccessor.count == slices[instance.MeshIndex].FaceCount * 3);

            var prim = new GltfMeshPrimitive(vertexAccessorIndex, indexAccessorIndex, matIndex);
            var mesh = new GltfMesh { primitives = [prim] };

            var node = new GltfNode(transform, Data.meshes.Count);

            Data.meshes.Add(mesh);
            Data.nodes.Add(node);
        }
    }

    public GltfData Build(List<byte> bytes)
    {
        var vertexByteSize = Vertices.Count * 3 * sizeof(float);
        var indexByteSize = Faces.Count * 3 * sizeof(int);

        var totalBufferSize = vertexByteSize + indexByteSize;

        var vertexBufferView = new GltfBufferView(0, 0, vertexByteSize, GltfTargets.ARRAY_BUFFER, string.Empty);
        var indexBufferView = new GltfBufferView(0, vertexByteSize, indexByteSize, GltfTargets.ELEMENT_ARRAY_BUFFER, string.Empty);

        Data.bufferViews.Add(vertexBufferView);
        Data.bufferViews.Add(indexBufferView);
        
        var buffer = new GltfBuffer 
        {
            byteLength = totalBufferSize,
        };
        Data.buffers.Add(buffer);

        bytes.AddRange(BitConverter.GetBytes((uint)totalBufferSize));
        bytes.AddRange(GltfWriter.BinChunkType);

        foreach (var x in Vertices)
        {
            bytes.AddRange(BitConverter.GetBytes(x.X.Value));
            bytes.AddRange(BitConverter.GetBytes(x.Y.Value));
            bytes.AddRange(BitConverter.GetBytes(x.Z.Value));
        }
        foreach (var x in Faces)
        {
            bytes.AddRange(BitConverter.GetBytes(x.A.Value));
            bytes.AddRange(BitConverter.GetBytes(x.B.Value));
            bytes.AddRange(BitConverter.GetBytes(x.C.Value));
        }

        var scene = new GltfScene();
        for (var i = 0; i < Data.nodes.Count; i++)
            scene.nodes.Add(i);
        Data.scenes = [scene];
        return Data;
    }
}
