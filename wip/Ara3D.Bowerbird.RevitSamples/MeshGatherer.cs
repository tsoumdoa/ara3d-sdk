using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace Ara3D.Bowerbird.RevitSamples;

public class MeshGatherer
{
    public RevitBimDataBuilder RevitBimDataBuilder;

    public MeshGatherer(RevitBimDataBuilder builder)
    {
        RevitBimDataBuilder = builder;
    }

    public sealed record GeometryPart
    (
        Transform Transform,
        Mesh Mesh,
        ElementId MaterialId
    );

    public sealed record Geometry
    (
        Document SourceDocument,
        long ElementId,
        ElementId DefaultMaterialId,
        IReadOnlyList<GeometryPart> Parts
    );

    public readonly Dictionary<(Document, string), IReadOnlyList<GeometryPart>> SymbolCache = new();

    public readonly Dictionary<Document, List<Geometry>> ElementGeometries = new ();

    public IEnumerable<Mesh> GetMeshes()
    {
        foreach (var g in ElementGeometries.SelectMany(kv => kv.Value))
        {
            if (g == null)
                continue;
            foreach (var p in g.Parts)
                if (p?.Mesh != null)
                    yield return p.Mesh;
        }
    }

    public void CollectMeshes(Document doc, Options options, bool recurseLinks, Transform parent)
    {
        var current = new List<Geometry>();
        ElementGeometries.Add(doc, current);

        options ??= DefaultGeometryOptions();

        var elems = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .ToElements();

        foreach (var e in elems)
        {
            if (e?.Id == null) continue;
            current.Add(ComputeGeometry(e, parent, options));
        }

        if (recurseLinks)
        {
            foreach (var rli in new FilteredElementCollector(doc)
                         .OfClass(typeof(RevitLinkInstance))
                         .Cast<RevitLinkInstance>())
            {
                var linkDoc = rli.GetLinkDocument();
                if (linkDoc is null) continue; // unloaded/placeholder
                var linkTransform = rli.GetTotalTransform();
                CollectMeshes(linkDoc, options, false, linkTransform);
            }
        }
    }

    public IEnumerable<GeometryPart> GetOrComputeCachedGeometryParts(GeometryInstance gi, Transform transform, Options options)
    {
        using var symbolId = gi.GetSymbolGeometryId();
        if (symbolId == null)
        {
            // From what I have read we use GetInstanceGeometry() when things have been cut or modified, and the symbol geometry is not applicable 
            return ComputeGeometryParts(gi.GetInstanceGeometry(), transform, options);
        }

        var stringId = symbolId.AsUniqueIdentifier();
            
        // Check if we have to fill out the cache for this string id 
        if (!SymbolCache.ContainsKey((gi.GetDocument(), stringId)))
        {
            var geometryElement = gi.GetSymbolGeometry();
            var parts = ComputeGeometryParts(geometryElement, Transform.Identity, options);
            SymbolCache.Add((gi.GetDocument(), stringId), parts);
        }

        return SymbolCache[(gi.GetDocument(), stringId)]
            .Select(mp => mp with { Transform = transform.Multiply(mp.Transform) });
    }

    public Geometry ComputeGeometry(Element e, Transform transform, Options options)
    {
        var matId = ResolveFallbackMaterialId(e);
        var parts = ComputeGeometryParts(e, transform, options);
        if (parts.Count == 0) return null;
        return new Geometry(e.Document, e.Id.Value, matId, parts);
    }

    public IReadOnlyList<GeometryPart> ComputeGeometryParts(Element e, Transform transform, Options options)
    {
        return ComputeGeometryParts(e.get_Geometry(options), transform, options);
    }

    public IReadOnlyList<GeometryPart> ComputeGeometryParts(GeometryElement ge, Transform transform, Options options)
    {
        var list = new List<GeometryPart>();
        if (ge is null) return list;

        // Recursive flatten
        void Accumulate(GeometryObject go, Transform currentTf)
        {
            switch (go)
            {
                case Solid s when s.Faces.Size > 0:
                    list.AddRange(ToGeometryParts(s, currentTf));
                    break;

                case Mesh m when m.Vertices.Count > 0:
                    list.Add(new GeometryPart(currentTf, m, m.MaterialElementId));
                    break;

                case GeometryInstance gi:
                    var instTf = currentTf.Multiply(gi.Transform);
                    list.AddRange(GetOrComputeCachedGeometryParts(gi, instTf, options));
                    break;

                case Curve c:
                    // ignore non-surface geometry
                    break;

                case PolyLine pl:
                    // ignore wireframe
                    break;

                case Point p:
                    // ignore points
                    break;

                case Face f:
                    // Very rare to get a stray Face here, but handle it
                    list.Add(ToGeometryPart(f, currentTf));
                    break;

                default:
                    // Other types (e.g., Line, Arc, etc.) are not tessellated here
                    break;
            }
        }

        foreach (var go in ge) 
            Accumulate(go, transform);

        return list;
    }
    
    public static GeometryPart ToGeometryPart(Face f, Transform tf)
        => new(tf, f.Triangulate(), f.MaterialElementId);

    public static IEnumerable<GeometryPart> ToGeometryParts(Solid s, Transform tf) 
        => s.Faces.OfType<Face>().Select(f => ToGeometryPart(f, tf));

    private static ElementId ResolveFallbackMaterialId(Element e)
    {
        // Try element's own material parameter first (works for some categories)
        var pMat = e.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
        if (pMat is { StorageType: StorageType.ElementId })
        {
            var id = pMat.AsElementId();
            if (id != ElementId.InvalidElementId) 
                return id;
        }

        // Category's material is the usual fallback (often Invalid)
        if (e.Category != null)
        {
            var mat = e.Category.Material;
            if (mat != null) 
                return mat.Id;
        }

        return ElementId.InvalidElementId;
    }

    public static Options DefaultGeometryOptions()
        => new()
        {
            DetailLevel = ViewDetailLevel.Medium,
            ComputeReferences = false,
            IncludeNonVisibleObjects = false,
        };
}
