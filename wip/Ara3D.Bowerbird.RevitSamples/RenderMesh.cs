using System;
using System.Collections.Generic;
using Ara3D.Collections;
using Ara3D.Geometry;
using Ara3D.Memory;

namespace Ara3D.Bowerbird.RevitSamples;

/// <summary>
/// Allocates a mesh appropriate for usage with low-level graphics APIs.
/// Memory if
/// </summary>
public class RenderMesh 
{
    public readonly IBuffer<RenderVertex> Vertices;
    public readonly IBuffer<Integer> Indices;

    public RenderMesh(IBuffer<RenderVertex> vertices, IBuffer<Integer> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public static RenderMesh Create(
        IReadOnlyList<Point3D> vertices,
        IReadOnlyList<Integer> indices = null,
        IReadOnlyList<Vector3> normals = null,
        IReadOnlyList<Vector2> uvs = null,
        IReadOnlyList<Color32> colors = null)
    {
        var vertexCount = vertices.Count;
        if (vertexCount == 0)
            throw new Exception("Empty meshes not supported");

        var indexBuffer = indices?.ToArray().Fix();

        normals = normals ?? Vector3.Default.Repeat(vertexCount);
        if (normals.Count != vertexCount)
            throw new InvalidOperationException($"Normals count {normals.Count} must match vertices count {vertexCount}.");

        uvs = uvs ?? Vector2.Default.Repeat(vertexCount);
        if (uvs.Count != vertexCount)
            throw new InvalidOperationException($"UVs count {uvs.Count} must match vertices count {vertexCount}.");

        colors = colors ?? new Color32(128, 128, 128).Repeat(vertexCount);
        if (colors.Count != vertexCount)
            throw new InvalidOperationException($"Colors count {colors.Count} must match vertices count {vertexCount}.");

        var renderVertices = new AlignedMemory<RenderVertex>(vertexCount);
        for (var i = 0; i < vertexCount; i++)
        {
            renderVertices[i] =
                new RenderVertex(vertices[i], normals[i], uvs[i], colors[i]);
        }

        return new RenderMesh(renderVertices, indexBuffer);
    }
}