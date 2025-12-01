using System.Runtime.InteropServices;
using Ara3D.Geometry;

namespace Ara3D.Bowerbird.RevitSamples;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RenderVertex
{
    public RenderVertex(Vector3 position)
        : this(position, default, default, default)
    { }

    public RenderVertex(Vector3 position, Vector3 normal)
        : this(position, normal, default, default)
    { }

    public RenderVertex(Vector3 position, Vector3 normal, Vector2 uv)
        : this(position, normal, uv, default)
    { }

    public RenderVertex(Vector3 position, Color32 color)
        : this(position, default, default, color)
    { }

    public RenderVertex(Vector3 position, Vector3 normal, Vector2 uv, Color32 color)
    {
        PX = (float)position.X;
        PY = (float)position.Y;
        PZ = (float)position.Z;
        NX = (float)normal.X;
        NY = (float)normal.Y;
        NZ = (float)normal.Z;
        U = (float)uv.X;
        V = (float)uv.Y;
        RGBA = color;
    }

    public float PX, PY, PZ; // Position = 12 bytes
    public float NX, NY, NZ; // Normal = 6 bytes
    public float U, V; // UV = 4 bytes
    public Color32 RGBA; // Colors = 4 bytes

    public static implicit operator RenderVertex(Vector3 v) => new RenderVertex(v);
    public static implicit operator RenderVertex((Vector3 v, Vector3 n) args) => new RenderVertex(args.v, args.n);
    public static implicit operator RenderVertex((Vector3 v, Vector3 n, Vector2 uv) args) => new RenderVertex(args.v, args.n, args.uv);
    public static implicit operator RenderVertex((Vector3 v, Vector3 n, Vector2 uv, Color32 c) args) => new RenderVertex(args.v, args.n, args.uv, args.c);
    public static implicit operator RenderVertex((Vector3 v, Color32 c) args) => new RenderVertex(args.v, args.c);

    public Vector3 Position => (PX, PY, PZ);
    public Vector3 Normal => (NX, NY, NZ);
    public Vector2 UV => (U, V);
    public Color32 Color => RGBA;
}