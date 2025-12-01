using System.Diagnostics;
using Ara3D.Geometry;
using Ara3D.IO.BFAST;
using Ara3D.Memory;
using Ara3D.MemoryMappedFiles;
using Ara3D.Utils;

namespace Ara3D.Models;

public static class RenderSceneSerializer
{
    public static string[] BufferNames = new[]
    {
        nameof(RenderScene.Vertices),
        nameof(RenderScene.Indices),
        nameof(RenderScene.MeshSlices),
        nameof(RenderScene.Instances),
        nameof(RenderScene.InstanceGroups),
    };

    public static unsafe void Save(RenderScene renderScene, FilePath filePath)
    {
        var sizes = new[]
        {
            renderScene.Vertices.Bytes.Count,
            renderScene.Indices.Bytes.Count,
            renderScene.MeshSlices.Bytes.Count,
            renderScene.Instances.Bytes.Count,
            renderScene.InstanceGroups.Bytes.Count,
        };

        Debug.Assert(sizes[0] % sizeof(Point3D) == 0);
        Debug.Assert(sizes[1] % 4 == 0);
        Debug.Assert(sizes[2] % sizeof(MeshSliceStruct) == 0);
        Debug.Assert(sizes[3] % InstanceStruct.Size == 0);
        Debug.Assert(sizes[4] % InstanceGroupStruct.Size == 0);

        var ptrs = new[]             
        {
            renderScene.Vertices.Bytes.Ptr,
            renderScene.Indices.Bytes.Ptr,
            renderScene.MeshSlices.Bytes.Ptr,
            renderScene.Instances.Bytes.Ptr,
            renderScene.InstanceGroups.Bytes.Ptr,
        };

        long OnBuffer(Stream stream, int index, string name, long bytesToWrite)
        {
            var ptr = ptrs[index];
            var size = sizes[index];
            Debug.Assert(bytesToWrite == size);
            while (true)
            {
                var tmp = Math.Min(size, int.MaxValue);
                var span = new ReadOnlySpan<byte>(ptr, (int)tmp);
                stream.Write(span);
                size -= tmp;
                if (size <= 0)
                    break;
            }
            stream.Flush();
            return bytesToWrite;
        }

        BFast.Write((string)filePath, BufferNames, sizes.Select(sz => (long)sz), OnBuffer);
    }

    public static unsafe RenderScene Load(FilePath fp)
    {
        AlignedMemory<float> vertices = null;
        AlignedMemory<uint> indices = null;
        AlignedMemory<MeshSliceStruct> meshes = null;
        AlignedMemory<InstanceStruct> instances = null;
        AlignedMemory<InstanceGroupStruct> groups = null;
            
        void OnView(string name, MemoryMappedView view, int index)
        {
            byte* srcPointer = null;
            view.Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref srcPointer);
            try
            {
                IBuffer buffer = index switch
                {
                    0 => vertices = new AlignedMemory<float>(view.Size / sizeof(float)),
                    1 => indices = new AlignedMemory<uint>(view.Size / sizeof(uint)),
                    2 => meshes = new AlignedMemory<MeshSliceStruct>(view.Size / sizeof(MeshSliceStruct)),
                    3 => instances = new AlignedMemory<InstanceStruct>(view.Size / InstanceStruct.Size),
                    4 => groups = new AlignedMemory<InstanceGroupStruct>(view.Size / InstanceGroupStruct.Size),
                    _ => throw new Exception("Unrecognized memory buffer")
                };

                srcPointer += view.Accessor.PointerOffset;
                Buffer.MemoryCopy(srcPointer, buffer.GetPointer(), view.Size, view.Size);
            }
            finally
            {
                view.Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }

        BFastReader.Read(fp, OnView);
            
        return new RenderScene(vertices, indices, meshes, instances, groups);
    }
}