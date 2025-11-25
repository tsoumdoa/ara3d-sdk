using Ara3D.BimOpenSchema;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Document = Autodesk.Revit.DB.Document;
using Parameter = Ara3D.BimOpenSchema.Parameter;
using static Ara3D.BimOpenSchema.CommonRevitParameters;
using RevitParameter = Autodesk.Revit.DB.Parameter;

namespace Ara3D.Bowerbird.RevitSamples;

public readonly record struct DocumentKey
(
    string Title,
    string FileName
);

public readonly record struct ElementKey(
    DocumentKey DocKey, 
    long ElementId
);

public class RevitBimDataBuilder
{
    public RevitBimDataBuilder(Document rootDocument, bool includeLinks, bool processDoc = true) 
    {
        IncludeLinks = includeLinks;
        CreateCommonDescriptors();
        if (processDoc)
            ProcessDocument(rootDocument);
    }

    public class StructuralLayer
    {
        public int LayerIndex;
        public MaterialFunctionAssignment Function;
        public double Width;
        public ElementId MaterialId;
        public bool IsCore;
    }

    public BimDataBuilder Builder = new();
    public bool IncludeLinks;
    public Document CurrentDocument;
    public DocumentIndex CurrentDocumentIndex;
    public DocumentKey CurrentDocumentKey;
    public Dictionary<string, DescriptorIndex> DescriptorLookup = new();
    public Dictionary<ElementKey, EntityIndex> ProcessedEntities = new();
    public Dictionary<DocumentKey, DocumentIndex> ProcessedDocuments = new();
    public Dictionary<long, EntityIndex> ProcessedCategories = new();

    public EntityIndex GetEntityIndex(Document doc, long entityId)
        => GetEntityIndex(GetDocumentKey(doc), entityId);

    public EntityIndex GetEntityIndex(DocumentKey key, long entityId)
        => ProcessedEntities[GetElementKey(key, entityId)];

    public EntityIndex GetEntityIndex(ElementKey key)
        => ProcessedEntities[key];

    public static (XYZ min, XYZ max)? GetBoundingBoxMinMax(Element element, View view = null)
    {
        if (element == null) return null;
        var bb = element.get_BoundingBox(view);
        return bb == null ? null : (bb.Min, bb.Max);
    }

    public PointIndex AddPoint(BimDataBuilder bdb, XYZ xyz)
        => bdb.AddPoint(new(xyz.X, xyz.Y, xyz.Z));

    public void CreateCommonDescriptors()
    {
        foreach (var p in CommonRevitParameters.GetParameters())
        {
            var desc = Builder.AddDescriptor(p.Name, "", "RevitAPI", p.Type);
            DescriptorLookup.Add(p.Name, desc);
        }
    }

    public List<StructuralLayer> GetLayers(HostObjAttributes host)
    {
        var compound = host.GetCompoundStructure();
        if (compound == null) return [];
        var r = new List<StructuralLayer>();
        for (var i = 0; i < compound.LayerCount; i++)
        {
            r.Add(new StructuralLayer()
            {
                Function = compound.GetLayerFunction(i),
                IsCore = compound.IsCoreLayer(i),
                LayerIndex = i,
                MaterialId = compound.GetMaterialId(i),
                Width = compound.GetLayerWidth(i)
            });
        }

        return r;
    }

    public void AddParameter(EntityIndex ei, Parameter p, string val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, string val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, DateTime val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, DateTime val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        var str = val.ToString("o", CultureInfo.InvariantCulture);
        Builder.AddParameter(ei, str, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, EntityIndex val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, EntityIndex val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Entity) throw new Exception($"Expected entity not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, PointIndex val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, PointIndex val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Point) throw new Exception($"Expected point not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, int val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, int val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, bool val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, bool val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val ? 1 : 0, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, double val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, double val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Double) throw new Exception($"Expected double not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddTypeAsParameter(EntityIndex ei, object o)
    {
        AddParameter(ei, ObjectTypeName, o.GetType().Name);
    }

    public EntityIndex ProcessCategory(Category category)
    {
        if (!ProcessedCategories.TryGetValue(category.Id.Value, out var result))
            return result;

        var r = Builder.AddEntity(category.Id.Value, category.Id.ToString(), CurrentDocumentIndex, category.Name,
            category.BuiltInCategory.ToString());

        AddParameter(r, ObjectTypeName, category.GetType().Name);
        AddParameter(r, CategoryCategoryType, category.CategoryType.ToString());
        AddParameter(r,  CategoryBuiltInType, category.BuiltInCategory.ToString());

        foreach (Category subCategory in category.SubCategories)
        {
            var subCatId = ProcessCategory(subCategory);
            Builder.AddRelation(subCatId, r, RelationType.ChildOf);
        }

        return r;
    }

    public void ProcessCompoundStructure(EntityIndex ei, HostObjAttributes host)
    {
        var layers = GetLayers(host);
        if (layers == null) return;

        foreach (var layer in layers)
        {
            var index = layer.LayerIndex;
            var layerEi = Builder.AddEntity(
                0, 
                $"{host.UniqueId}${index}", 
                CurrentDocumentIndex, 
                $"{host.Name}[{index}]", 
                layer.Function.ToString());

            AddParameter(layerEi, LayerIndex, index);
            AddParameter(layerEi, LayerFunction, layer.Function.ToString());
            AddParameter(layerEi, LayerWidth, layer.Width);
            AddParameter(layerEi, LayerIsCore, layer.IsCore);

            var matId = layer.MaterialId;
            if (matId != ElementId.InvalidElementId)
            {
                var matIndex = ProcessElement(matId);
                AddParameter(layerEi, LayerMaterialId, matIndex);
                Builder.AddRelation(layerEi, matIndex, RelationType.HasMaterial);
            }

            AddTypeAsParameter(layerEi, layer);
            Builder.AddRelation(ei, layerEi, RelationType.HasLayer);
        }
    }

    public void ProcessMaterial(EntityIndex ei, Material m)
    {
        var color = m.Color;            
        AddParameter(ei, MaterialColorRed, color.Red / 255.0);
        AddParameter(ei, MaterialColorGreen, color.Green / 255.0);
        AddParameter(ei, MaterialColorBlue, color.Blue / 255.0);

        AddParameter(ei, MaterialTransparency, m.Transparency / 100.0);
        AddParameter(ei, MaterialShininess, m.Shininess / 128.0);
        AddParameter(ei, MaterialSmoothness, m.Smoothness / 100.0);
        AddParameter(ei, MaterialCategory, m.MaterialCategory);
        AddParameter(ei, MaterialClass, m.MaterialClass);
    }

    public void ProcessFamily(EntityIndex ei, Family f)
    {
        AddParameter(ei, FamilyStructuralCodeName, f.StructuralCodeName);
        AddParameter(ei, FamilyStructuralMaterialType, f.StructuralMaterialType.ToString());
    }
    
    public void ProcessFamilyInstance(EntityIndex ei, FamilyInstance f)
    {
        var typeId = f.GetTypeId();
        if (typeId != ElementId.InvalidElementId)
        {
            var type = ProcessElement(typeId);
            AddParameter(ei, FIFamilyType, type);
            Builder.AddRelation(ei, type, RelationType.InstanceOf);
        }

        var toRoom = f.ToRoom;
        if (toRoom != null && toRoom.IsValidObject)
            AddParameter(ei, FIToRoom, ProcessElement(toRoom));

        var fromRoom = f.FromRoom;
        if (fromRoom != null && fromRoom.IsValidObject)
            AddParameter(ei, FIFromRoom, ProcessElement(fromRoom));

        var host = f.Host;
        if (host != null && host.IsValidObject)
        {
            var hostIndex = ProcessElement(host);
            AddParameter(ei, FIHost, hostIndex);
            Builder.AddRelation(ei, hostIndex, RelationType.HostedBy);
        }

        var space = f.Space;
        if (space != null && space.IsValidObject)
        {
            var spaceIndex = ProcessElement(space);
            AddParameter(ei, FISpace, spaceIndex);
            Builder.AddRelation(ei, spaceIndex, RelationType.ContainedIn);
        }

        var room = f.Room;
        if (room != null && room.IsValidObject)
        {
            var roomIndex = ProcessElement(room);
            AddParameter(ei, FIRoom, roomIndex);
            Builder.AddRelation(ei, roomIndex, RelationType.ContainedIn);
        }

        var matId = f.StructuralMaterialId;
        if (matId != ElementId.InvalidElementId)
        {
            var matIndex = ProcessElement(matId);
            AddParameter(ei, FIStructuralMaterial, matIndex);
            Builder.AddRelation(ei, matIndex, RelationType.HasMaterial);
        }

        AddParameter(ei, FIStructuralUsage, f.StructuralUsage.ToString());
        AddParameter(ei, FIStructuralMaterialType, f.StructuralMaterialType.ToString());
        AddParameter(ei, FIStructuralType, f.StructuralType.ToString());
    }

    public void ProcessRoom(EntityIndex ei, Room room)
    {
        AddParameter(ei, RoomBaseOffset, room.BaseOffset);
        AddParameter(ei, RoomLimitOffset, room.LimitOffset);
        AddParameter(ei, RoomNumber, room.Number);
        AddParameter(ei, RoomUnboundedHeight, room.UnboundedHeight);
        if (room.UpperLimit != null && room.UpperLimit.IsValidObject)
            AddParameter(ei, RoomUpperLimit, ProcessElement(room.UpperLimit));
        AddParameter(ei, RoomVolume, room.Volume);
    }
    
    public void ProcessLevel(EntityIndex ei, Level level)
    {
        AddParameter(ei, LevelElevation, level.Elevation);
        AddParameter(ei, LevelProjectElevation, level.ProjectElevation);
    }

    public void ProcessMaterials(EntityIndex ei, Element e)
    {
        var matIds = e.GetMaterialIds(false);
        foreach (var id in matIds)
        {
            var matId = ProcessElement(id);
            Builder.AddRelation(ei, matId, RelationType.HasMaterial);
        }
    }
    
    public static string GetUnitLabel(RevitParameter p)
    {
        var spec = p.Definition.GetDataType();
        if (!UnitUtils.IsMeasurableSpec(spec))
            return "";
        var unitId = p.GetUnitTypeId();
        return UnitUtils.GetTypeCatalogStringForUnit(unitId);
    }

    public void ProcessParameters(EntityIndex entityIndex, Element element)
    {
        foreach (RevitParameter p in element.Parameters)
        {
            if (p == null) continue;
            var def = p.Definition;
            if (def == null) continue;
            var groupId = def.GetGroupTypeId();
            var groupLabel = LabelUtils.GetLabelForGroup(groupId);
            var unitLabel = GetUnitLabel(p);
            switch (p.StorageType)
            {
                case StorageType.Integer:
                    AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.Int), p.AsInteger());
                    break;
                case StorageType.Double:
                    AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.Double), p.AsDouble());
                    break;
                case StorageType.String:
                    AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.String), p.AsString());
                    break;
                case StorageType.ElementId:
                    {
                        var val = p.AsElementId();
                        if (val == ElementId.InvalidElementId)
                            continue;
                        var e = element.Document.GetElement(val);
                        if (e == null)
                            continue;

                        // We recursively process the element 
                        var valIndex = ProcessElement(e);
                        AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.Entity), valIndex);
                    }
                    break;
                case StorageType.None:
                default:
                    AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.String), p.AsValueString());
                    break;
            }
        }
    }
    
    public EntityIndex ProcessElement(ElementId id)
    {
        if (id == null || id == ElementId.InvalidElementId)
            return (EntityIndex)(-1);

        var key = GetElementKey(CurrentDocumentKey, id.Value);
        if (ProcessedEntities.TryGetValue(key, out var found))
            return found;

        var element = CurrentDocument.GetElement(id);
        if (element == null)
            return (EntityIndex)(-1);

        return ProcessNewElement(key, element);
    }

    public static bool TryGetLocationEndpoints(
        LocationCurve lc,
        out XYZ startPoint,
        out XYZ endPoint)
    {
        startPoint = null;
        endPoint = null;
        var curve = lc?.Curve;
        if (curve == null) return false;
        if (!curve.IsBound) return false;
        startPoint = curve.GetEndPoint(0);
        endPoint = curve.GetEndPoint(1);
        return true;
    }

    public EntityIndex ProcessElement(Element element)
    {
        if (element == null || !element.IsValidObject)
            return (EntityIndex)(-1);

        var key = GetElementKey(CurrentDocumentKey, element.Id.Value);
        if (ProcessedEntities.TryGetValue(key, out var found))
            return found;

        return ProcessNewElement(key, element);
    }

    public EntityIndex ProcessNewElement(ElementKey key, Element e)
    {
        var category = e.Category;
        var catName = (category != null && category.IsValid) ? category.Name : "";

        var entityIndex = Builder.AddEntity(e.Id.Value, e.UniqueId, CurrentDocumentIndex, e.Name, catName);
        ProcessedEntities.Add(key, entityIndex);

        AddTypeAsParameter(entityIndex, e);

        if (category != null && category.IsValid)
        {
            var catIndex = ProcessCategory(category);
            AddParameter(entityIndex, ElementCategory, catIndex);
            Builder.AddRelation(entityIndex, catIndex, RelationType.ContainedIn);
        }

        ProcessParameters(entityIndex, e);
        ProcessMaterials(entityIndex, e);

        var bounds = GetBoundingBoxMinMax(e);
        if (bounds.HasValue)
        {
            var min = AddPoint(Builder, bounds.Value.min);
            var max = AddPoint(Builder, bounds.Value.max);
            AddParameter(entityIndex, ElementBoundsMin, min);
            AddParameter(entityIndex, ElementBoundsMax, max);
        }

        var levelId = e.LevelId;
        if (levelId != ElementId.InvalidElementId)
        {
            var levelIndex = ProcessElement(levelId);
            AddParameter(entityIndex, ElementLevel, levelIndex);
            Builder.AddRelation(entityIndex, levelIndex, RelationType.ContainedIn);
        }

        var assemblyInstanceId = e.AssemblyInstanceId;
        if (assemblyInstanceId != ElementId.InvalidElementId)
        {
            var assemblyIndex = ProcessElement(assemblyInstanceId);
            AddParameter(entityIndex, ElementAssemblyInstance, assemblyIndex);
            Builder.AddRelation(entityIndex, assemblyIndex, RelationType.PartOf);
        }
        
        var location = e.Location;
        if (location != null)
        {
            if (location is LocationPoint lp)
            {
                AddParameter(entityIndex, ElementLocationPoint, AddPoint(Builder, lp.Point));
            }

            if (location is LocationCurve lc)
            {
                if (TryGetLocationEndpoints(lc, out var startPoint, out var endPoint))
                {
                    AddParameter(entityIndex, ElementLocationStartPoint, AddPoint(Builder, startPoint));
                    AddParameter(entityIndex, ElementLocationEndPoint, AddPoint(Builder, endPoint));
                }
            }
        }

        if (e.CreatedPhaseId != ElementId.InvalidElementId)
        {
            var createdPhase = ProcessElement(e.CreatedPhaseId);
            AddParameter(entityIndex, ElementCreatedPhase, createdPhase);
        }

        if (e.DemolishedPhaseId != ElementId.InvalidElementId)
        {
            var demolishedPhase = ProcessElement(e.DemolishedPhaseId);
            AddParameter(entityIndex, ElementDemolishedPhase, demolishedPhase);
        }

        var designOption = e.DesignOption;
        if (designOption != null && designOption.IsValidObject)
        {
            var doIndex = ProcessElement(designOption);
            Builder.AddRelation(entityIndex, doIndex, RelationType.MemberOf);
            AddParameter(entityIndex, ElementDesignOption, doIndex);
        }

        var groupId = e.GroupId;
        if (groupId != ElementId.InvalidElementId)
        {
            var group = ProcessElement(groupId);
            Builder.AddRelation(entityIndex, group, RelationType.MemberOf);
            AddParameter(entityIndex, ElementGroup, group);
        }

        if (e.WorksetId != null)
        {
            AddParameter(entityIndex, ElementWorksetId, e.WorksetId.IntegerValue);
        }

        if (e.ViewSpecific)
        {
            AddParameter(entityIndex, ElementIsViewSpecific, true);
        }

        if (e.OwnerViewId != ElementId.InvalidElementId)
        {
            var view = ProcessElement(e.OwnerViewId);
            AddParameter(entityIndex, ElementOwnerView, view);
        }

        if (e is HostObjAttributes host)
            ProcessCompoundStructure(entityIndex, host);

        if (e is Room r)
            ProcessRoom(entityIndex, r);

        if (e is Level level)
            ProcessLevel(entityIndex, level);

        if (e is Family family)
            ProcessFamily(entityIndex, family);

        if (e is FamilyInstance familyInstance)
            ProcessFamilyInstance(entityIndex, familyInstance);

        if (e is Material material)
            ProcessMaterial(entityIndex, material);

        return entityIndex;
    }

    public static ElementKey GetElementKey(DocumentKey docKey, long id)
        => new(docKey, id);

    public static ElementKey GetElementKey(DocumentKey docKey, ElementId id)
        => GetElementKey(docKey, id.Value);

    public static ElementKey GetElementKey(Document d, ElementId id)
        => GetElementKey(GetDocumentKey(d), id);

    public static DocumentKey GetDocumentKey(Document d)
    {
        if (d == null)
            throw new Exception("Unexpected null document");
        if (d.IsDetached)
            // NOTE: the title and path will be empty if detached. Not sure what we can do as a backup plan.  
            throw new Exception("Cannot process detached documents");
        return new(d.Title, System.IO.Path.GetFileNameWithoutExtension(d.PathName)?.ToLowerInvariant() ?? "");
    }

    public void ProcessDocument(Document d)
    {
        var key = GetDocumentKey(d);
        if (ProcessedDocuments.ContainsKey(key))
            return;

        CurrentDocument = d;
        CurrentDocumentIndex = Builder.AddDocument(d.Title, d.PathName);
        CurrentDocumentKey = GetDocumentKey(CurrentDocument);

        // NOTE: this creates a pseudo-entity for the document, which is used so that we can associate parameters and meta-data with it. 
        var ei = Builder.AddEntity(ProcessedDocuments.Count, CurrentDocument.CreationGUID.ToString(), CurrentDocumentIndex, d.Title, "__DOCUMENT__");
        ProcessedDocuments.Add(key, CurrentDocumentIndex);

        var siteLocation = CurrentDocument.SiteLocation;

        var fi = new FileInfo(d.PathName);
        if (fi.Exists)
        {
            var saveDate = fi.LastWriteTimeUtc;
            AddParameter(ei, DocumentLastSaveTime, saveDate);
            var fileInfo = BasicFileInfo.Extract(d.PathName);
            var docVersion = fileInfo.GetDocumentVersion();
            if (docVersion != null)
            {
                AddParameter(ei, DocumentSaveCount, docVersion.NumberOfSaves);
            }
        }

        AddParameter(ei, DocumentPath, CurrentDocument.PathName);
        AddParameter(ei, DocumentTitle, CurrentDocument.Title);
        AddParameter(ei, DocumentIsDetached, CurrentDocument.IsDetached);
        AddParameter(ei, DocumentIsLinked, CurrentDocument.IsLinked);

        if (CurrentDocument.IsWorkshared)
            AddParameter(ei, DocumentWorksharingGuid, CurrentDocument.WorksharingCentralGUID.ToString());
        AddParameter(ei, DocumentCreationGuid, CurrentDocument.CreationGUID.ToString());
        AddParameter(ei, DocumentElevation, siteLocation.Elevation);
        AddParameter(ei, DocumentLatitude, siteLocation.Latitude);
        AddParameter(ei, DocumentLongitude, siteLocation.Longitude);
        AddParameter(ei, DocumentPlaceName, siteLocation.PlaceName);
        AddParameter(ei, DocumentWeatherStationName, siteLocation.WeatherStationName);
        
        // TODO: this is a doujble 
        //AddParameter(ei, DocumentTimeZone, siteLocation.TimeZone);

        var projectInfo = CurrentDocument.ProjectInformation;

        AddParameter(ei, ProjectAddress, projectInfo.Address);
        AddParameter(ei, ProjectAuthor, projectInfo.Author);
        AddParameter(ei, ProjectBuildingName, projectInfo.BuildingName);
        AddParameter(ei, ProjectClientName, projectInfo.ClientName);
        AddParameter(ei, ProjectIssueDate, projectInfo.IssueDate);
        AddParameter(ei, ProjectName, projectInfo.Name);
        AddParameter(ei, ProjectNumber, projectInfo.Number);
        AddParameter(ei, ProjectOrgDescription, projectInfo.OrganizationDescription);
        AddParameter(ei, ProjectOrgName, projectInfo.OrganizationName);
        AddParameter(ei, ProjectStatus, projectInfo.Status);

        foreach (var e in CurrentDocument.GetElements())
            ProcessElement(e);

        if (IncludeLinks)
            foreach (var linkedDoc in d.GetLinkedDocuments())
                ProcessDocument(linkedDoc);
    }
}