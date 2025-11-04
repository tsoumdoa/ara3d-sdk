using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ara3D.Geometry;
using Ara3D.Utils;

namespace Ara3D.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct InstanceStruct
{
    // –––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // Static properties 
    public static readonly uint Size = (uint)sizeof(InstanceStruct);

    // –––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // Static initializer - for debugging 
    static InstanceStruct()
        => Debug.Assert(Size == 64);

    // –––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // Fields 

    public Vector4 Column0;
    public Vector4 Column1;
    public Vector4 Column2;
    public int MeshIndex; //  4 bytes
    // NOTE: this is used by the rendering manager ... multiple scenes can be rendered at a time. 
    public int SceneIndex;  //  4 bytes
    public uint PackedColor; // 4 bytes
    public uint MetallicRoughness; // (byte 0 == Metallic, byte 1 == Roughness, bytes 3-4 unused)

    // –––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // Constructors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct(Matrix4x4 transform,
        int meshIndex,
        Color color,
        float metallic,
        float roughness)
    {
        Matrix4x4 = transform;
        MeshIndex = meshIndex;
        Color = color;
        Metallic = metallic;
        Roughness = roughness;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct(Matrix4x4 transform,
        int meshIndex,
        Material mat)
        : this(transform, meshIndex, mat.Color, mat.Metallic, mat.Roughness)
    { }

    //==
    // Properties 

    public float Metallic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MetallicRoughness.GetByte0().ToNormalizedFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => MetallicRoughness = MetallicRoughness.SetByte0(value.ToByteFromNormalized());
    }

    public float Roughness
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MetallicRoughness.GetByte1().ToNormalizedFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => MetallicRoughness = MetallicRoughness.SetByte1(value.ToByteFromNormalized());
    }

    public Color Color
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(
            PackedColor.GetByte0().ToNormalizedFloat(),
            PackedColor.GetByte1().ToNormalizedFloat(),
            PackedColor.GetByte2().ToNormalizedFloat(),
            PackedColor.GetByte3().ToNormalizedFloat());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => PackedColor = PackedColor.SetBytes(
            value.R.Value.ToByteFromNormalized(),
            value.G.Value.ToByteFromNormalized(),
            value.B.Value.ToByteFromNormalized(),
            value.A.Value.ToByteFromNormalized());
    }

    public float Alpha 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => PackedColor.GetByte3().ToNormalizedFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Color = Color.WithA(value);
    }

    public bool Transparent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Alpha < 0.99f;
    }

    public Matrix4x4 Matrix4x4
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(
            Column0.X, Column1.X, Column2.X, 0f,
            Column0.Y, Column1.Y, Column2.Y, 0f,
            Column0.Z, Column1.Z, Column2.Z, 0f,
            Column0.W, Column1.W, Column2.W, 1f);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Column0 = (value.M11, value.M21, value.M31, value.M41);
            Column1 = (value.M12, value.M22, value.M32, value.M42);
            Column2 = (value.M13, value.M23, value.M33, value.M43);
        }
    }

    public Material Material
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Color, Metallic, Roughness);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Color = value.Color;
            Metallic = value.Metallic;
            Roughness = value.Roughness;
        }
    }

    //------------
    // With functions

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithMatrix(Matrix4x4 matrix)
    {
        var r = this;
        r.Matrix4x4 = matrix;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithColor(Color color)
    {
        var r = this;
        r.Color = color;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithAlpha(float alpha) 
    {
        var r = this;
        r.Alpha = alpha;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithRoughness(float roughness)
    {
        var r = this;
        r.Roughness = roughness;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithMetallic(float metallic)
    {
        var r = this;
        r.Metallic = metallic;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithMaterial(Material material)
    {
        var r = this;
        r.Material = material;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceStruct WithMeshIndex(int meshIndex)
    {
        var r = this;
        r.MeshIndex = meshIndex;
        return r;
    }

    //------------
    // Helper function

    public InstanceStruct Transform(Matrix4x4 matrix)
    {
        var r = this;
        r.Matrix4x4 *= matrix;
        return r;
    }
}