using Autodesk.Revit.DB;
using System;
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
    Element Element,
    ElementKey ElementKey,
    Material? DefaultMaterial,
    IReadOnlyList<GeometryPart> Parts
);

/// <summary>
/// Collects all meshes
/// </summary>
public class MeshGatherer
{
    public BimOpenSchemaRevitBuilder BimOpenSchemaRevitBuilder;
    public Document CurrentDocument { get; private set; }
    public int CurrentDocumentKey { get; private set; }
    public HashSet<int> ProcessedDocuments { get; } = [];
    public List<Mesh> MeshList { get; } = [];
    public List<Geometry> Geometries { get; private set; } = [];
    private readonly Dictionary<string, IReadOnlyList<GeometryPart>> _symbolCache = new();

    public MeshGatherer(BimOpenSchemaRevitBuilder revitBuilder)
    {
        BimOpenSchemaRevitBuilder = revitBuilder;
    }

    public static int GetDocumentKey(Document d)
        => BimOpenSchemaRevitBuilder.GetDocumentKey(d);

    public void CollectMeshes(Document doc, Options options, bool recurseLinks, Transform parent)
    {
        if (options.View != null)
        {
            CollectMeshesByView(doc, options, recurseLinks, parent);
            return;
        }

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
                .ToElements()
                .Where(e => e is not RevitLinkInstance);

            foreach (var e in elems)
            {
                if (e?.Id == null) continue;
                var g = ComputeGeometry(e, parent, options);
                if (g != null)
                    Geometries.Add(g);
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
                    CollectMeshes(linkDoc, options, true, parent.Multiply(linkTransform));
                }
            }
        }
        finally
        {
            CurrentDocument = previousDocument;
            CurrentDocumentKey = previousDocumentKey;
        }
    }

    // NOTE: not a recursive call, like the CollectMeshes call. 
    public void CollectMeshesByView(Document doc, Options options, bool recurseLinks, Transform parent)
    {
        var documentKey = GetDocumentKey(doc);
        if (ProcessedDocuments.Contains(documentKey))
            return;
        CurrentDocument = doc;
        CurrentDocumentKey = documentKey;
        ProcessedDocuments.Add(CurrentDocumentKey);

        var view = options.View;

        // host elements visible in the view
        var elems = new FilteredElementCollector(doc, view.Id)
            .WhereElementIsNotElementType()
            .Where(e => e is not RevitLinkInstance);

        foreach (var e in elems)
        {
            var g = ComputeGeometry(e, parent, options);
            if (g != null) Geometries.Add(g);
        }

        if (!recurseLinks) return;

        // link instances visible in the host view
        var linkInstances = new FilteredElementCollector(doc, view.Id)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>();

        foreach (var rli in linkInstances)
        {
            var linkDoc = rli.GetLinkDocument();
            if (linkDoc is null) continue;

            var newDocumentKey = GetDocumentKey(linkDoc);
            if (ProcessedDocuments.Contains(newDocumentKey))
                continue;

            CurrentDocument = linkDoc;
            CurrentDocumentKey = newDocumentKey;
            ProcessedDocuments.Add(CurrentDocumentKey);

            var linkXf = parent.Multiply(rli.GetTransform());

            // linked elements visible *through this link instance* in the host view (Revit 2024+)
            var linkedElems = new FilteredElementCollector(doc, view.Id, rli.Id)
                .WhereElementIsNotElementType();

            foreach (var le in linkedElems)
            {
                // le.Document should be the linkDoc (i.e., element comes from the linked model)
                var g = ComputeGeometry(le, linkXf, options);
                if (g != null) Geometries.Add(g);
            }
        }
    }

    public Geometry ComputeGeometry(Element e, Transform transform, Options options)
    {
        try
        {
            var elementKey = new ElementKey(CurrentDocumentKey, e.Id.Value);
            var material = e.ResolveFallbackMaterial();
            var geometryElement = e.get_Geometry(options);
            if (geometryElement == null) return null;
            var parts = new List<GeometryPart>();
            TraverseElementGeometry(geometryElement, transform, parts);
            if (parts.Count == 0) return null;
            return new Geometry(e, elementKey, material, parts);
        }
        catch
        {
            return null;
        }
    }

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
                    if (ShouldKeep(CurrentDocument, solid))
                        AddSolidMeshes(solid, transform, parts);
                    break;

                case Mesh mesh:
                    if (ShouldKeep(CurrentDocument, mesh))
                        AddMeshInstance(mesh, transform, parts);
                    break;

                case GeometryInstance gi:
                    ProcessInstanceWithCaching(gi, transform, parts);
                    break;

                case GeometryElement subGeom:
                    TraverseElementGeometry(subGeom, transform, parts);
                    break;
            }
        }
    }   

    public void ProcessInstanceWithCaching(GeometryInstance gi, Transform worldFromParent, List<GeometryPart> parts)
    {
        var templates = GetOrBuildSymbolTemplates(gi, worldFromParent);

        var worldFromSymbol = worldFromParent
            .Multiply(gi.Transform);

        foreach (var template in templates)
        {
            var worldTransform = worldFromSymbol.Multiply(template.Transform);
            parts.Add(template with { Transform = worldTransform });
        }
    }

    public string GetSymbolCacheKey(SymbolGeometryId symbolId)
    {
        if (symbolId == null) return null;
        var symbolElementId = symbolId.SymbolId;
        var symbol = CurrentDocument.GetElement(symbolElementId);
        if (symbol == null) return null;
        return $"{CurrentDocumentKey}_{symbolElementId.Value}";
    }

    private IReadOnlyList<GeometryPart> GetOrBuildSymbolTemplates(GeometryInstance gi, Transform worldFromParent)
    {
        if (gi == null) return [];

        var symbolId = gi.GetSymbolGeometryId();
        var key = GetSymbolCacheKey(symbolId);
        if (string.IsNullOrEmpty(key))
            return [];

        if (_symbolCache.TryGetValue(key, out var existing))
            return existing;

        var templates = new List<GeometryPart>();

        var symbolGeom = gi.GetSymbolGeometry();
        BuildSymbolTemplates(symbolGeom, Transform.Identity, templates);
        _symbolCache[key] = templates;
        return templates;
    }

    public void BuildSymbolTemplates(GeometryElement geom, Transform transform, List<GeometryPart> templates)
    {
        if (geom == null) 
            return;
        foreach (var obj in geom)
        {
            switch (obj)
            {
                case Solid solid:
                    if (ShouldKeep(CurrentDocument, solid))
                        AddSolidMeshes(solid, transform, templates);
                    break;

                case Mesh mesh:
                    if (ShouldKeep(CurrentDocument, mesh))
                        AddMeshInstance(mesh, transform, templates);
                    break;

                case GeometryInstance nestedGi:
                    BuildSymbolTemplates(
                        nestedGi.GetSymbolGeometry(), 
                        transform.Multiply(nestedGi.Transform), templates);
                    break;

                case GeometryElement subGeom:
                    BuildSymbolTemplates(subGeom, transform, templates);
                    break;
            }
        }
    }

    public int AddMesh(Mesh mesh)
    {
        MeshList.Add(mesh);
        return MeshList.Count - 1;
    }

    public void AddSolidMeshes(Solid solid, Transform transform, List<GeometryPart> parts)
    {
        foreach (Face face in solid.Faces)
            AddGeometryPart(
                face?.Triangulate(), 
                transform,
                parts,
                CurrentDocument.GetAra3DMaterial(face));
    }

    public void AddGeometryPart(Mesh mesh, Transform transform, List<GeometryPart> parts, Material? mat)
    {
        try
        {
            if (mesh == null || mesh.NumTriangles == 0)
                return;

            var index = AddMesh(mesh);
            var part = new GeometryPart(transform, index, mat);
            parts.Add(part);
        }
        catch
        {
            // Swallow exception
        }
    }

    private void AddMeshInstance(Mesh mesh, Transform transform, List<GeometryPart> parts)
        => AddGeometryPart(mesh, transform, parts, CurrentDocument.GetAra3DMaterial(mesh));

    public static bool ShouldKeep(Document doc, GeometryObject obj)
    {
        var styleId = obj.GraphicsStyleId;
        if (styleId == ElementId.InvalidElementId)
            return true;

        var style = doc.GetElement(styleId) as GraphicsStyle;
        if (style == null)
            return true;

        var cat = style.GraphicsStyleCategory;
        if (cat == null)
            return true;

        // Explicitly skip light source subcategories
        var catName = cat.Name ?? string.Empty;
        if (catName.Equals("Light Source", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}

