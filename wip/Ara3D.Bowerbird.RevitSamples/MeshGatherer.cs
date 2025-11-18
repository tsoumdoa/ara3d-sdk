using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Material = Ara3D.Models.Material;

namespace Ara3D.Bowerbird.RevitSamples;

/// <summary>
/// A mesh with an index, a symbol and a transform  
/// </summary>
public sealed record GeometryPart
(
    Transform Transform,
    int MeshIndex,
    Material? Material
);

/// <summary>
/// A geometry is a set of geometric parts associated with an element and a document.
/// It has a default material.  
/// </summary>
public sealed record Geometry
(
    DocumentKey SourceDocumentKey,
    long ElementIdValue,
    Material? DefaultMaterial,
    IReadOnlyList<GeometryPart> Parts
);

/// <summary>
/// Used for uniquely identify a symbol ID by combining it with a document key 
/// </summary>
public readonly record struct GeometrySymbolKey
(
    DocumentKey DocumentKey,
    string SymbolId
);

/// <summary>
/// Collects all meshes
/// </summary>
public class MeshGatherer
{
    public RevitBimDataBuilder RevitBimDataBuilder;
    public Document CurrentDocument { get; private set; }
    public DocumentKey CurrentDocumentKey { get; private set; }
    public HashSet<DocumentKey> ProcessedDocuments { get; } = [];
    public List<Mesh> MeshList { get; } = [];
    public List<Geometry> Geometries { get; } = [];

    public MeshGatherer(RevitBimDataBuilder builder)
    {
        RevitBimDataBuilder = builder;
    }

    private readonly Dictionary<
        GeometrySymbolKey,
        IReadOnlyList<GeometryPart>> _symbolCache = new();

    public static DocumentKey GetDocumentKey(Document d)
        => RevitBimDataBuilder.GetDocumentKey(d);

    public void CollectMeshes(Document doc, Options options, bool recurseLinks, Transform parent)
    {
        var newDocumentKey = GetDocumentKey(doc);
        if (ProcessedDocuments.Contains(newDocumentKey))
            return;

        var previousDocumentKey = CurrentDocumentKey;
        var previousDocument = CurrentDocument;
        CurrentDocument = doc;
        CurrentDocumentKey = newDocumentKey;
        ProcessedDocuments.Add(CurrentDocumentKey);

        try
        {
            var elems = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var e in elems)
            {
                if (e?.Id == null) continue;
                Geometries.Add(ComputeGeometry(e, parent, options));
            }

            if (recurseLinks)
            {
                foreach (var rli in new FilteredElementCollector(doc)
                             .OfClass(typeof(RevitLinkInstance))
                             .Cast<RevitLinkInstance>())
                {
                    var linkDoc = rli.GetLinkDocument();
                    if (linkDoc is null)
                        continue;

                    var linkTransform = rli.GetTransform();
                    CollectMeshes(linkDoc, options, false, parent.Multiply(linkTransform));
                }
            }
        }
        finally
        {
            CurrentDocument = previousDocument;
            CurrentDocumentKey = previousDocumentKey;
        }
    }

    public Geometry ComputeGeometry(Element e, Transform transform, Options options)
    {
        var docKey = GetDocumentKey(e.Document);
        var material = ResolveFallbackMaterial(e);
        var geometryElement = e.get_Geometry(options);
        if (geometryElement == null) return null;
        var parts = new List<GeometryPart>();
        TraverseElementGeometry(geometryElement, transform, parts);
        if (parts.Count == 0) return null;
        return new Geometry(docKey, e.Id.Value, material, parts);
    }

    public int AddMesh(Mesh mesh)
    {
        MeshList.Add(mesh);
        return MeshList.Count - 1;
    }

    public GeometryPart ToGeometryPart(Face f, Transform tf)
    {
        if (f == null) return null;
        var mesh = f.Triangulate();
        if (mesh == null) return null;
        if (mesh.NumTriangles == 0) return null;
        return new(tf, AddMesh(mesh), GetMaterial(f));
    }

    public IEnumerable<GeometryPart> ToGeometryParts(Solid s, Transform tf)
        => s.Faces.OfType<Face>().Select(f => ToGeometryPart(f, tf)).Where(gp => gp != null);

    //==
    // Material code

    public static Material? ToMaterial(PbrMaterialInfo pbr)
        => pbr == null
            ? null
            : new Material(pbr.BaseColor ?? pbr.ShadingColor, (float)(pbr.Metallic ?? 0),
                (float)(pbr.Roughness ?? 0));

    public static Material? ToMaterial(Document doc, ElementId? materialId)
    {
        if (doc == null)
            return null;
        if (materialId == null)
            return null;
        if (materialId == ElementId.InvalidElementId)
            return null;
        var pbrMatInfo = doc.GetPbrInfo(materialId.Value);
        return ToMaterial(pbrMatInfo);
    }

    public Material? GetMaterial(Face f)
        => ToMaterial(CurrentDocument, f?.MaterialElementId);

    public Material? GetMaterial(Mesh m)
        => ToMaterial(CurrentDocument, m?.MaterialElementId);

    public static Material? ResolveFallbackMaterial(Element e)
        => e == null ? null : ToMaterial(e.Document, ResolveFallbackMaterialId(e));

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

    /// <summary>
    /// Traverses geometry in model space and populates meshes + TransformedMesh instances.
    /// </summary>
    public void TraverseElementGeometry(
        GeometryElement geom,
        Transform transform,
        List<GeometryPart> parts)
    {
        if (geom == null)
            return;

        foreach (var obj in geom)
        {
            switch (obj)
            {
                case Solid solid:
                    AddSolidMeshes(solid, transform, parts);
                    break;

                case Mesh mesh:
                    AddMeshInstance(mesh, transform, parts);
                    break;

                case GeometryInstance gi:
                    ProcessInstanceNoCaching(gi, transform, parts);
                    //ProcessInstanceWithCaching(gi, transform, parts);
                    break;

                case GeometryElement subGeom:
                    TraverseElementGeometry(subGeom, transform, parts);
                    break;

                default:
                    // Ignore curves, points, etc. Extend as needed.
                    break;
            }
        }
    }

    public void ProcessInstanceNoCaching(GeometryInstance gi, Transform transform, List<GeometryPart> parts)
    {
        TraverseElementGeometry(gi.GetInstanceGeometry(), transform, parts);
    }

    public void ProcessInstanceWithCaching(GeometryInstance gi, Transform transform, List<GeometryPart> parts)
    {
        var templates = GetOrBuildSymbolTemplates(gi);

        // Symbol templates are in symbol space. 
        var instToWorldTransform = transform.Multiply(gi.Transform);
        foreach (var tmpl in templates)
        {
            parts.Add(tmpl with { Transform = instToWorldTransform.Multiply(tmpl.Transform) });
        }
    }

    public void BuildSymbolTemplates(
        GeometryElement geom,
        Transform transform,
        List<GeometryPart> templates)
    {
        foreach (var obj in geom)
        {
            switch (obj)
            {
                case Solid solid:
                    AddSolidMeshes(solid, transform, templates);
                    break;

                case Mesh mesh:
                    AddMeshInstance(mesh, transform, templates);
                    break;

                case GeometryInstance nestedGi:
                {
                    // When encountering a geometry instance as part of a symbol template,
                    // we get it as instance geometry 
                    var nestedGeom = nestedGi.GetSymbolGeometry();
                    if (nestedGeom == null)
                        break;

                    // When retrieving instance geometry we do not need to further transform it,
                    // It is already in the coordinate space of the owner (I believe)
                    BuildSymbolTemplates(nestedGeom, transform.Multiply(nestedGi.Transform), templates);
                    break;
                }

                case GeometryElement subGeom:
                    BuildSymbolTemplates(subGeom, transform, templates);
                    break;

                default:
                    // Ignore curves, points, etc.
                    break;
            }
        }
    }

    /// <summary>
    /// Returns the template meshes for a symbol geometry, and caches it if this is the first time. 
    /// Each template stores:
    /// - MeshIndex into the global meshes list
    /// - ToSymbol: transform from mesh-local coordinates to the symbol root coordinates
    /// - Material of the object. 
    /// This runs at most once per unique SymbolGeometryId.
    /// </summary>
    private IReadOnlyList<GeometryPart> GetOrBuildSymbolTemplates(GeometryInstance gi)
    {
        if (gi == null) return [];

        // SymbolGeometryId is a value object; Revit API doesn't document
        // it as nullable. We still guard against weird cases defensively.
        var symbolId = gi.GetSymbolGeometryId();
        var symbolIdStr = symbolId?.AsUniqueIdentifier();

        if (string.IsNullOrEmpty(symbolIdStr))
            // Rare case
            return [];

        var key = new GeometrySymbolKey(CurrentDocumentKey, symbolIdStr);
        if (_symbolCache.TryGetValue(key, out var existing))
            return existing;

        var templates = new List<GeometryPart>();
        var symbolGeom = gi.GetSymbolGeometry();
        if (symbolGeom != null)
        {
            BuildSymbolTemplates(symbolGeom, Transform.Identity, templates);
        }

        _symbolCache[key] = templates;
        return templates;
    }

    /// <summary>
    /// Can be used to store a symbol template part (transform is relative to symbol space)
    /// Or to store a model part (transform is to world space)
    /// </summary>
    public void AddSolidMeshes(
        Solid solid,
        Transform transform,
        List<GeometryPart> parts)
    {
        foreach (Face face in solid.Faces)
        {
            try
            {
                if (face == null)
                    continue;

                var faceMesh = face.Triangulate();
                if (faceMesh == null || faceMesh.NumTriangles == 0)
                    continue;

                var index = AddMesh(faceMesh);
                parts.Add(new GeometryPart(transform, index, GetMaterial(face)));
            }
            catch
            {
                // We eat Exception on purpose
            }
        }
    }

    /// <summary>
    /// Can be used to store a symbol template part (transform is relative to symbol space)
    /// Or to store a model part (transform is to world space)
    /// </summary>
    private void AddMeshInstance(
        Mesh mesh,
        Transform transform,
        List<GeometryPart> parts)
    {
        if (mesh == null || mesh.NumTriangles == 0)
            return;

        var index = AddMesh(mesh);
        parts.Add(new GeometryPart(transform, index, GetMaterial(mesh)));
    }
}

