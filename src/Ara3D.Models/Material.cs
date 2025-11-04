using Ara3D.Geometry;
using System.Runtime.CompilerServices;

namespace Ara3D.Models;

public record struct Material(Color Color, float Metallic, float Roughness)
{
    public static Color DefaultColor = new(0.5f, 0.5f, 0.5f, 1f);
    public static float DefaultMetallic = 0.1f;
    public static float DefaultRoughness = 0.5f;
    public static Material Default = new(DefaultColor, DefaultMetallic, DefaultRoughness);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material WithColor(Color color) 
        => this with { Color = color };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material WithMetallic(float metallic) 
        => this with { Metallic = metallic };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material WithRoughness(float roughness) 
        => this with { Roughness = roughness };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material WithAlpha(float alpha) 
        => WithColor(Color.WithA(alpha));
    
    public float Red
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Color.R; 
    }

    public float Green
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Color.G;
    }

    public float Blue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Color.B;
    }

    public float Alpha
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Color.A;
    }
}