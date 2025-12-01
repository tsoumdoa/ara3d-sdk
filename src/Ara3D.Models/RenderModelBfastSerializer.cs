using System.Diagnostics;
using Ara3D.Geometry;
using Ara3D.IO.BFAST;
using Ara3D.Memory;
using Ara3D.MemoryMappedFiles;
using Ara3D.Utils;

namespace Ara3D.Models;

public static class RenderModelBfastSerializer
{
    public static string[] BufferNames = new[]
    {
        nameof(RenderModel3D.Vertices),
        nameof(RenderModel3D.Indices),
        nameof(RenderModel3D.MeshSlices),
        nameof(RenderModel3D.Instances),
        nameof(RenderModel3D.InstanceGroups),
    };

    public static unsafe void Save(RenderModel3D renderModel3D, FilePath filePath)
    {
        var sizes = new[]
        {
            renderModel3D.Vertices.Bytes.Count,
            renderModel3D.Indices.Bytes.Count,
            renderModel3D.MeshSlices.Bytes.Count,
            renderModel3D.Instances.Bytes.Count,
            renderModel3D.InstanceGroups.Bytes.Count,
        };

        Debug.Assert(sizes[0] % sizeof(Point3D) == 0);
        Debug.Assert(sizes[1] % 4 == 0);
        Debug.Assert(sizes[2] % sizeof(MeshSliceStruct) == 0);
        Debug.Assert(sizes[3] % InstanceStruct.Size == 0);
        Debug.Assert(sizes[4] % InstanceGroupStruct.Size == 0);

        var ptrs = new[]             
        {
            renderModel3D.Vertices.Bytes.Ptr,
            renderModel3D.Indices.Bytes.Ptr,
            renderModel3D.MeshSlices.Bytes.Ptr,
            renderModel3D.Instances.Bytes.Ptr,
            renderModel3D.InstanceGroups.Bytes.Ptr,
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

    public static unsafe RenderModel3D Load(FilePath fp)
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
            
        return new RenderModel3D(vertices, indices, meshes, instances, groups);
    }
}