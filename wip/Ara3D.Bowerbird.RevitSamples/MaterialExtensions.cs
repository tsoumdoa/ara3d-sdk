using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Color = Ara3D.Geometry.Color;
using Material = Autodesk.Revit.DB.Material;

namespace Ara3D.Bowerbird.RevitSamples;

public static class MaterialExtensions
{
    /// <summary>
    /// Retrieves the basic PBR data from a material ID.
    /// </summary>
    public static PbrMaterialInfo? GetPbrInfo(this Document doc, long materialId)
    {
        // 1. Look up the Material
        var mat = doc.GetElement(new ElementId((int)materialId)) as Material;
        if (mat is null) return null;

        // 2. Always-available legacy shading colour
        var legacyOpacity = 1f - mat.Transparency / 100f;
        var legacyColor = new Color(
            mat.Color.Red / 255f, 
            mat.Color.Green / 255f, 
            mat.Color.Blue / 255f, 
            legacyOpacity);

        // 3. Try to reach the rendering (appearance) asset
        var assetEl = doc.GetElement(mat.AppearanceAssetId) as AppearanceAssetElement;
        if (assetEl is null)
            return new PbrMaterialInfo(mat.Name, legacyColor, null, null, null, null);

        var asset = assetEl.GetRenderingAsset();

        // 4. Map the parameters we care about
        Color? baseCol = null;
        Color? emissive = null;
        double? metallic = null;
        double? roughness = null;
        double? opacity = null;

        for (var i=0; i < asset.Size; i++)
        {
            var prop = asset[i];

            if (prop is AssetPropertyDoubleArray4d col)
            {
                var c = ToDrawingColor(col);
                switch (prop.Name)
                {
                    case "generic_diffuse":
                    case "UnifiedDiffuse":
                        baseCol = c; break;

                    case "generic_emission":
                    case "UnifiedEmission":
                        emissive = c; break;
                }
            }
            else if (prop is AssetPropertyDouble d)
            {
                switch (prop.Name)
                {
                    case "generic_metallic":
                    case "UnifiedMetallic":
                        metallic = d.Value; break;

                    case "generic_roughness":
                    case "UnifiedRoughness":
                        roughness = d.Value; break;

                    case "generic_opacity":
                    case "UnifiedOpacity":
                        opacity = d.Value; break;
                }
            }
        }

        var alpha = opacity.HasValue 
            ? (float)opacity.Value 
            : legacyOpacity;
        if (baseCol != null)
            baseCol = baseCol.Value.WithA(alpha);

        legacyColor = legacyColor.WithA(alpha);
        return new PbrMaterialInfo(mat.Name, legacyColor, baseCol, metallic, roughness, emissive);
    }

    private static Color ToDrawingColor(AssetPropertyDoubleArray4d col)
    {
        var dbls = col.GetValueAsDoubles();
        var r = (float)dbls[0];
        var g = (float)dbls[1];
        var b = (float)dbls[2];
        var a = (float)dbls[3];
        return new Color(r, g, b, a);
    }

    public static Models.Material? ToAra3DMaterial(this PbrMaterialInfo pbr)
        => pbr == null
            ? null
            : new Models.Material(pbr.BaseColor ?? pbr.ShadingColor, (float)(pbr.Metallic ?? 0),
                (float)(pbr.Roughness ?? 0));

    public static Models.Material? ToAra3DMaterial(this Document doc, ElementId? materialId)
    {
        if (doc == null)
            return null;
        if (materialId == null)
            return null;
        if (materialId == ElementId.InvalidElementId)
            return null;
        var pbrMatInfo = doc.GetPbrInfo(materialId.Value);
        return ToAra3DMaterial(pbrMatInfo);
    }

    public static Models.Material? GetAra3DMaterial(this Document self, Face f)
        => f == null ? null : ToAra3DMaterial(self, f?.MaterialElementId);

    public static Models.Material? GetAra3DMaterial(this Document self, Mesh m)
        => m == null ? null : ToAra3DMaterial(self, m?.MaterialElementId);

    public static Models.Material? ResolveFallbackMaterial(this Element e)
        => e == null ? null : ToAra3DMaterial(e.Document, ResolveFallbackMaterialId(e));

    public static ElementId ResolveFallbackMaterialId(Element e)
    {
        // Try element's own material parameter first 
        var pMat = e.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
        if (pMat is { StorageType: StorageType.ElementId })
        {
            var id = pMat.AsElementId();
            if (id != ElementId.InvalidElementId)
                return id;
        }

        // Use the category material as a fallback 
        var mat = e.Category?.Material;
        return mat != null
            ? mat.Id
            : ElementId.InvalidElementId;
    }
}