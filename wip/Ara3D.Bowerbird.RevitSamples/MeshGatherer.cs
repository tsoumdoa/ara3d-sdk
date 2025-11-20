using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Ara3D.Utils;
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
    private readonly Dictionary<string, IReadOnlyList<GeometryPart>> _symbolCache = new();

    public MeshGatherer(RevitBimDataBuilder builder)
    {
        RevitBimDataBuilder = builder;
    }

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
                .ToElements()
                .Where(e => e is not RevitLinkInstance);

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

    public Geometry ComputeGeometry(Element e, Transform transform, Options options)
    {
        var docKey = GetDocumentKey(e.Document);
        var material = e.ResolveFallbackMaterial();
        var geometryElement = e.get_Geometry(options);
        if (geometryElement == null) return null;
        var parts = new List<GeometryPart>();
        TraverseElementGeometry(geometryElement, transform, parts);
        if (parts.Count == 0) return null;
        return new Geometry(docKey, e.Id.Value, material, parts);
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
                    AddSolidMeshes(solid, transform, parts);
                    break;

                case Mesh mesh:
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
        return $"{CurrentDocumentKey.FileName}_{CurrentDocumentKey.Title}_{symbolElementId.Value}";
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
                    AddSolidMeshes(solid, transform, templates);
                    break;

                case Mesh mesh:
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

    public HashSet<ElementKey> GetVisibleSet(View hostView)
    {
        var hostDoc = hostView.Document;
        var viewId = hostView.Id;
        var hostDocKey = GetDocumentKey(hostDoc);
        var visibleSet = new HashSet<ElementKey>();

        var hostCollector = new FilteredElementCollector(hostDoc, viewId)
            .WhereElementIsNotElementType()
            .ToElementIds();

        foreach (var elementId in hostCollector)
        {
            var key = new ElementKey(hostDocKey, elementId.Value);
            visibleSet.Add(key);
        }

        var linkInstances = new FilteredElementCollector(hostDoc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .ToList();

        foreach (var rli in linkInstances)
        {
            var linkDoc = rli.GetLinkDocument();
            if (linkDoc == null) continue;

            var linkDocKey = GetDocumentKey(linkDoc);

            var visibleLinkedElementIds = new FilteredElementCollector(hostDoc, viewId, rli.Id)
                .WhereElementIsNotElementType()
                .ToElementIds();

            foreach (var elementId in visibleLinkedElementIds)
            {
                var key = new ElementKey(linkDocKey, elementId.Value);
                visibleSet.Add(key);
            }
        }

        return visibleSet;
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
}

