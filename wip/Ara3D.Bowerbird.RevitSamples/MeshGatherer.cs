using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Material = Ara3D.Models.Material;
using Autodesk.Revit.DB.IFC;
using System; // For IFCUtils.UsesInstanceGeometry

namespace Ara3D.Bowerbird.RevitSamples;

public sealed record GeometryPart(
    Transform Transform,
    int MeshIndex,
    Material? Material
);

public sealed record Geometry
(
    DocumentKey SourceDocumentKey,
    long ElementIdValue,
    Material? DefaultMaterial,
    IReadOnlyList<GeometryPart> Parts
);

public sealed record GeometrySymbolKey
(
    DocumentKey DocumentKey,
    string SymbolId
);

public class MeshGatherer
{
    public RevitBimDataBuilder RevitBimDataBuilder;
    public Document CurrentDocument { get; private set;  }
    public DocumentKey CurrentDocumentKey { get; private set; }
    public HashSet<DocumentKey> ProcessedDocuments { get; } = [];
    public List<Mesh> MeshList { get; } = [];
    public List<Geometry> Geometries { get; }= [];

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
            options ??= DefaultGeometryOptions();

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

    public IEnumerable<GeometryPart> GetOrComputeCachedGeometryParts(GeometryInstance gi, Options options)
    {
        using var symbolId = gi.GetSymbolGeometryId();
        
        if (symbolId == null)
        {
            // When things have been cut or modified, the symbolId will return null
            // and the symbol geometry is not applicable so We use GetInstanceGeometry()
            // Geometry is in the coordinate system of the model.
            return ComputeGeometryParts(gi.GetInstanceGeometry(), Transform.Identity, options);
        }

        var stringId = symbolId.AsUniqueIdentifier();
        var key = new GeometrySymbolKey(GetDocumentKey(gi.GetDocument()), stringId);

        // Check if we have to fill out the cache for this string id 
        if (!_symbolCache.ContainsKey(key))
        {
            // Retrieve the geometry in local space of the symbol
            var geometryElement = gi.GetSymbolGeometry();

            // Get all of the geometric parts 
            var parts = ComputeGeometryParts(geometryElement, Transform.Identity, options);
            _symbolCache.Add(key, parts);
        }

        // Return the cached symbols, transformed appropriately 
        return _symbolCache[key];
    }

    public Geometry ComputeGeometry(Element e, Transform transform, Options options)
    {
        var docKey = GetDocumentKey(e.Document);
        var material = ResolveFallbackMaterial(e);
        var parts = ComputeGeometryParts(e, transform, options);
        if (parts.Count == 0) return null;
        return new Geometry(docKey, e.Id.Value, material, parts);
    }

    public IReadOnlyList<GeometryPart> ComputeGeometryParts(Element e, Transform transform, Options options)
    {
        return ComputeGeometryParts(e.get_Geometry(options), transform, options);
    }

    public void AccumulateGeometryParts(GeometryObject go, Transform transform, List<GeometryPart> list, Options options)
    {
        switch (go)
        {
            case Solid s when s.Faces.Size > 0:
                list.AddRange(ToGeometryParts(s, transform));
                break;

            case Mesh m when m.Vertices.Count > 0:
                list.Add(new GeometryPart(transform, AddMesh(m), GetMaterial(m)));
                break;

            case GeometryInstance gi:
                var instTf = transform.Multiply(gi.Transform);
                var parts = GetOrComputeCachedGeometryParts(gi, options);
                list.AddRange(parts.Select(p => p with { Transform = instTf.Multiply(p.Transform) }));
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
                var gp = ToGeometryPart(f, transform);
                if (gp != null)
                    list.Add(gp);
                break;

            default:
                // Other types (e.g., Line, Arc, etc.) are not tessellated here
                break;
        }
    }

    /// <summary>
    /// A geometry element contains a collection of geometric primitives. 
    /// </summary>
    public IReadOnlyList<GeometryPart> ComputeGeometryParts(GeometryElement ge, Transform transform, Options options)
    {
        var list = new List<GeometryPart>();
        if (ge == null) 
            return list;
        foreach (var go in ge) 
            AccumulateGeometryParts(go, transform, list, options);
        return list;
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

    public static Options DefaultGeometryOptions()
        => new()
        {
            DetailLevel = ViewDetailLevel.Medium,
            ComputeReferences = false,
            IncludeNonVisibleObjects = false,
        };

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

        // QUESTION: is there anything else that is appropriate as a fallback?

        // Use the category material as a fallback 
        var mat = e.Category?.Material;
        return mat != null 
            ? mat.Id 
            : ElementId.InvalidElementId;
    }

    /// <summary>
    /// Logical instance of a mesh in the model.
    /// MeshIndex indexes into the shared Mesh list.
    /// Transform maps from the mesh's local coordinates to model coordinates.
    /// </summary>
    public readonly record struct TransformedMesh(int MeshIndex, Transform Transform);

    public static class MeshExtractor
    {
        /// <summary>
        /// Mesh template used when expanding a symbol:
        /// - MeshIndex: index into the global meshes list
        /// - ToSymbol: transform from mesh-local coordinates to the symbol's root coordinates
        /// </summary>
        private readonly record struct TemplateMesh(int MeshIndex, Transform ToSymbol);

        /// <summary>
        /// Extracts meshes and instance transforms for the given elements.
        /// - meshes: shared raw Mesh objects (no transforms baked in)
        /// - instances: placement records pointing at meshes by index with full world transforms
        /// 
        /// Family instances that truly share symbol geometry are deduped via SymbolGeometryId.
        /// Family instances that use instance geometry fall back to instance geometry
        /// and are NOT deduped via SymbolGeometryId.
        /// </summary>
        public static void ExtractMeshes(
            Document doc,
            IEnumerable<ElementId> elementIds,
            Options options,
            out List<Mesh> meshes,
            out List<TransformedMesh> instances)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            if (options == null) throw new ArgumentNullException(nameof(options));

            meshes = new List<Mesh>();
            instances = new List<TransformedMesh>();

            // Cache: SymbolGeometryId.AsIdentifier(true) -> template meshes in symbol space
            var symbolTemplates = new Dictionary<string, List<TemplateMesh>>();

            foreach (var id in elementIds)
            {
                var elem = doc.GetElement(id);
                if (elem == null) continue;

                var geom = elem.get_Geometry(options);
                if (geom == null) continue;

                // Decide per element if symbol-level deduplication is valid.
                // For FamilyInstances that use instance geometry, we must not
                // assume that symbol geometry is shared.
                var  allowSymbolDedup = true;
                    //elem is FamilyInstance fi && !ExporterIFCUtils.UsesInstanceGeometry(fi);

                // Root is in model coordinates
                TraverseElementGeometry(
                    geom,
                    Transform.Identity,   // currentToModel
                    meshes,
                    instances,
                    symbolTemplates,
                    allowSymbolDedup);
            }
        }

        #region Element-side traversal (produces final TransformedMesh)

        /// <summary>
        /// Traverses geometry in model space and populates meshes + TransformedMesh instances.
        /// </summary>
        private static void TraverseElementGeometry(
            GeometryElement geom,
            Transform currentToModel,
            List<Mesh> meshes,
            List<TransformedMesh> instances,
            Dictionary<string, List<TemplateMesh>> symbolTemplates,
            bool allowSymbolDedupForThisElement)
        {
            foreach (var obj in geom)
            {
                switch (obj)
                {
                    case GeometryInstance gi:
                        HandleGeometryInstance(
                            gi,
                            currentToModel,
                            meshes,
                            instances,
                            symbolTemplates,
                            allowSymbolDedupForThisElement);
                        break;

                    case Solid solid when solid.Faces.Size > 0:
                        AddSolidMeshes(
                            solid,
                            currentToModel,
                            meshes,
                            instances);
                        break;

                    case Mesh mesh:
                        AddMeshInstance(
                            mesh,
                            currentToModel,
                            meshes,
                            instances);
                        break;

                    case GeometryElement subGeom:
                        TraverseElementGeometry(
                            subGeom,
                            currentToModel,
                            meshes,
                            instances,
                            symbolTemplates,
                            allowSymbolDedupForThisElement);
                        break;

                    default:
                        // Ignore curves, points, etc. Extend as needed.
                        break;
                }
            }
        }

        /// <summary>
        /// Handles a GeometryInstance, choosing between:
        /// - symbol-based templates with SymbolGeometryId (dedup path), or
        /// - instance geometry (no dedup) when the owning element uses instance geometry.
        /// </summary>
        private static void HandleGeometryInstance(
            GeometryInstance gi,
            Transform currentToModel,
            List<Mesh> meshes,
            List<TransformedMesh> instances,
            Dictionary<string, List<TemplateMesh>> symbolTemplates,
            bool allowSymbolDedupForThisElement)
        {
            if (!allowSymbolDedupForThisElement)
            {
                // This element has instance-specific geometry.
                // Use GetInstanceGeometry (already in instance coordinates)
                // and do NOT cache/dedup via SymbolGeometryId.
                GeometryElement instGeom = gi.GetInstanceGeometry();
                if (instGeom == null)
                    return;

                // Geometry from GetInstanceGeometry() is already in the
                // instance coordinate system, so currentToModel is enough
                // to get to model coordinates.
                TraverseElementGeometry(
                    instGeom,
                    currentToModel,
                    meshes,
                    instances,
                    symbolTemplates,
                    allowSymbolDedupForThisElement: false);

                return;
            }

            // Normal dedup path: use symbol geometry + SymbolGeometryId.
            var templates = GetOrBuildSymbolTemplates(
                gi,
                meshes,
                symbolTemplates);

            // instanceToModel: symbol-root -> model
            Transform instanceToModel = currentToModel.Multiply(gi.Transform);

            // Emit one TransformedMesh per template mesh
            foreach (var tmpl in templates)
            {
                // tmpl.ToSymbol maps mesh-local -> symbol-root
                // instanceToModel maps symbol-root -> model
                Transform worldTransform = instanceToModel.Multiply(tmpl.ToSymbol);
                instances.Add(new TransformedMesh(tmpl.MeshIndex, worldTransform));
            }
        }

        private static void AddSolidMeshes(
            Solid solid,
            Transform currentToModel,
            List<Mesh> meshes,
            List<TransformedMesh> instances)
        {
            foreach (Face face in solid.Faces)
            {
                if (face == null) continue;

                Mesh faceMesh = face.Triangulate();
                if (faceMesh == null || faceMesh.NumTriangles == 0)
                    continue;

                int index = meshes.Count;
                meshes.Add(faceMesh);

                // Mesh vertices are in the solid/element's native coordinates;
                // currentToModel maps that to model coordinates.
                instances.Add(new TransformedMesh(index, currentToModel));
            }
        }

        private static void AddMeshInstance(
            Mesh mesh,
            Transform currentToModel,
            List<Mesh> meshes,
            List<TransformedMesh> instances)
        {
            if (mesh == null || mesh.NumTriangles == 0)
                return;

            int index = meshes.Count;
            meshes.Add(mesh);
            instances.Add(new TransformedMesh(index, currentToModel));
        }

        #endregion

        #region Symbol-side template building (dedup path, no baked transforms)

        /// <summary>
        /// Returns (and caches) the template meshes for a symbol geometry.
        /// Each template stores:
        /// - MeshIndex into the global meshes list
        /// - ToSymbol: transform from mesh-local coordinates to the symbol root coordinates
        /// 
        /// This runs at most once per unique SymbolGeometryId.
        /// </summary>
        private static List<TemplateMesh> GetOrBuildSymbolTemplates(
            GeometryInstance gi,
            List<Mesh> meshes,
            Dictionary<string, List<TemplateMesh>> symbolTemplates)
        {
            // SymbolGeometryId is a value object; Revit API doesn't document
            // it as nullable. We still guard against weird cases defensively.
            var symbolId = gi.GetSymbolGeometryId();
            string key = symbolId?.AsUniqueIdentifier() ?? string.Empty;

            if (string.IsNullOrEmpty(key))
            {
                // Fallback: if for any reason we can't get a stable id,
                // treat this instance as non-dedupable and just use
                // symbol geometry directly (no caching).
                var templatesNoCache = new List<TemplateMesh>();
                var symGeom = gi.GetSymbolGeometry();
                if (symGeom != null)
                {
                    BuildSymbolTemplates(
                        symGeom,
                        Transform.Identity,
                        meshes,
                        templatesNoCache);
                }
                return templatesNoCache;
            }

            if (symbolTemplates.TryGetValue(key, out var existing))
                return existing;

            var templates = new List<TemplateMesh>();
            var symbolGeom = gi.GetSymbolGeometry();
            if (symbolGeom != null)
            {
                // Build templates with transforms expressed in symbol-root coordinates.
                BuildSymbolTemplates(
                    symbolGeom,
                    Transform.Identity, // toSymbol: geometry-local -> symbol-root
                    meshes,
                    templates);
            }

            symbolTemplates[key] = templates;
            return templates;
        }

        /// <summary>
        /// Recursively traverses symbol geometry and fills 'templates' with TemplateMesh entries.
        /// 
        /// - 'toSymbol' is the accumulated transform from the current geometry context
        ///   to the symbol's root coordinates (NOT to model coordinates).
        /// - Mesh vertices are left in their original local space; 'toSymbol'
        ///   describes how to place them in the symbol.
        /// </summary>
        private static void BuildSymbolTemplates(
            GeometryElement geom,
            Transform toSymbol,
            List<Mesh> meshes,
            List<TemplateMesh> templates)
        {
            foreach (var obj in geom)
            {
                switch (obj)
                {
                    case Solid solid when solid.Faces.Size > 0:
                        AddSymbolSolidMeshes(
                            solid,
                            toSymbol,
                            meshes,
                            templates);
                        break;

                    case Mesh mesh:
                        AddSymbolMesh(
                            mesh,
                            toSymbol,
                            meshes,
                            templates);
                        break;

                    case GeometryInstance nestedGi:
                        {
                            var nestedGeom = nestedGi.GetSymbolGeometry();
                            if (nestedGeom == null)
                                break;

                            // nestedToSymbol: geometry-local -> symbol-root
                            Transform nestedToSymbol = toSymbol.Multiply(nestedGi.Transform);
                            BuildSymbolTemplates(
                                nestedGeom,
                                nestedToSymbol,
                                meshes,
                                templates);
                            break;
                        }

                    case GeometryElement subGeom:
                        BuildSymbolTemplates(
                            subGeom,
                            toSymbol,
                            meshes,
                            templates);
                        break;

                    default:
                        // Ignore curves, points, etc.
                        break;
                }
            }
        }

        private static void AddSymbolSolidMeshes(
            Solid solid,
            Transform toSymbol,
            List<Mesh> meshes,
            List<TemplateMesh> templates)
        {
            foreach (Face face in solid.Faces)
            {
                if (face == null) continue;

                Mesh faceMesh = face.Triangulate();
                if (faceMesh == null || faceMesh.NumTriangles == 0)
                    continue;

                int index = meshes.Count;
                meshes.Add(faceMesh);

                templates.Add(new TemplateMesh(index, toSymbol));
            }
        }

        private static void AddSymbolMesh(
            Mesh mesh,
            Transform toSymbol,
            List<Mesh> meshes,
            List<TemplateMesh> templates)
        {
            if (mesh == null || mesh.NumTriangles == 0)
                return;

            int index = meshes.Count;
            meshes.Add(mesh);

            templates.Add(new TemplateMesh(index, toSymbol));
        }

        #endregion
    }
}
