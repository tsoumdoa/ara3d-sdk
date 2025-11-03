using Ara3D.Geometry;

namespace Ara3D.Models;

public record struct Material(Color Color, float Metallic, float Roughness)
{
    public static Color DefaultColor = new(0.5f, 0.5f, 0.5f, 1f);
    public static float DefaultMetallic = 0.1f;
    public static float DefaultRoughness = 0.5f;
    public static Material Default = new(DefaultColor, DefaultMetallic, DefaultRoughness);
    public Material WithColor(Color color) => this with { Color = color };
    public Material WithMetallic(float metallic) => this with { Metallic = metallic };
    public Material WithRoughness(float roughness) => this with { Roughness = roughness };
    public Material WithAlpha(float alpha) => WithColor(Color.WithA(alpha));
    public float Red => Color.R;
    public float Green => Color.G;
    public float Blue => Color.B;
    public float Alpha => Color.A;
}