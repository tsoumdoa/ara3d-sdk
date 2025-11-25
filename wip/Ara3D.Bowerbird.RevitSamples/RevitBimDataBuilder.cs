using Ara3D.BimOpenSchema;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB.Mechanical;
using Document = Autodesk.Revit.DB.Document;

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

    private DescriptorIndex _apiTypeDescriptor;

    private DescriptorIndex _elementLevel;
    private DescriptorIndex _elementLocation;
    private DescriptorIndex _elementLocationStartPoint;
    private DescriptorIndex _elementLocationEndPoint;
    private DescriptorIndex _elementBoundsMin;
    private DescriptorIndex _elementBoundsMax;
    private DescriptorIndex _elementAssemblyInstance;
    private DescriptorIndex _elementDesignOption;
    private DescriptorIndex _elementGroup;
    private DescriptorIndex _elementWorkset;
    private DescriptorIndex _elementCreatedPhase;
    private DescriptorIndex _elementDemolishedPhase;
    private DescriptorIndex _elementCategory;
    private DescriptorIndex _elementOwnerView;
    private DescriptorIndex _elementIsViewSpecific;

    private DescriptorIndex _familyInstanceToRoomDesc;
    private DescriptorIndex _familyInstanceFromRoomDesc;
    private DescriptorIndex _familyInstanceRoom;
    private DescriptorIndex _familyInstanceSpace;
    private DescriptorIndex _familyInstanceHost;
    private DescriptorIndex _familyInstanceFamilyType;
    private DescriptorIndex _familyInstanceStructuralUsage;
    private DescriptorIndex _familyInstanceStructuralMaterialType;
    private DescriptorIndex _familyInstanceStructuralMaterialId;
    private DescriptorIndex _familyInstanceStructuralType;

    private DescriptorIndex _familyStructuralCodeName;
    private DescriptorIndex _familyStructuralMaterialType;

    private DescriptorIndex _roomBaseOffset;
    private DescriptorIndex _roomLimitOffset;
    private DescriptorIndex _roomUnboundedHeight;
    private DescriptorIndex _roomVolume;
    private DescriptorIndex _roomUpperLimit;
    private DescriptorIndex _roomNumber;

    private DescriptorIndex _levelElevation;
    private DescriptorIndex _levelProjectElevation;

    private DescriptorIndex _materialColorRed;
    private DescriptorIndex _materialColorGreen;
    private DescriptorIndex _materialColorBlue;
    private DescriptorIndex _materialShininess;
    private DescriptorIndex _materialSmoothness;
    private DescriptorIndex _materialCategory;
    private DescriptorIndex _materialClass;
    private DescriptorIndex _materialTransparency;

    private DescriptorIndex _worksetKind;

    private DescriptorIndex _layerIndex;
    private DescriptorIndex _layerFunction;
    private DescriptorIndex _layerWidth;
    private DescriptorIndex _layerMaterialId;
    private DescriptorIndex _layerIsCore;

    private DescriptorIndex _documentTitle;
    private DescriptorIndex _documentPath;
    private DescriptorIndex _documentWorksharingGuid;
    private DescriptorIndex _documentCreationGuid;
    private DescriptorIndex _documentElevation;
    private DescriptorIndex _documentLatitude;
    private DescriptorIndex _documentLongitude;
    private DescriptorIndex _documentPlaceName;
    private DescriptorIndex _documentWeatherStationName;
    private DescriptorIndex _documentTimeZone;
    private DescriptorIndex _documentLastSaveTime;
    private DescriptorIndex _documentSaveCount;
    private DescriptorIndex _documentIsDetached;
    private DescriptorIndex _documentIsLinked;

    private DescriptorIndex _projectName;
    private DescriptorIndex _projectNumber;
    private DescriptorIndex _projectStatus;
    private DescriptorIndex _projectAddress;
    private DescriptorIndex _projectClientName;
    private DescriptorIndex _projectIssueDate;
    private DescriptorIndex _projectAuthor;
    private DescriptorIndex _projectBuildingName;
    private DescriptorIndex _projectOrgDescription;
    private DescriptorIndex _projectOrgName;

    private DescriptorIndex _categoryCategoryType;
    private DescriptorIndex _categoryBuiltInType;

    private void AddDesc(ref DescriptorIndex desc, string name, ParameterType pt)
    {
        desc = Builder.AddDescriptor(name, "", "RevitAPI", pt);
    }

    public void CreateCommonDescriptors()
    {
        AddDesc(ref _apiTypeDescriptor, CommonRevitParameters.ObjectTypeName, ParameterType.String);

        AddDesc(ref _elementLevel, CommonRevitParameters.ElementLevel, ParameterType.Entity);
        AddDesc(ref _elementLocation, CommonRevitParameters.ElementLocationPoint, ParameterType.Point);
        AddDesc(ref _elementLocationStartPoint, CommonRevitParameters.ElementLocationStartPoint, ParameterType.Point);
        AddDesc(ref _elementLocationEndPoint, CommonRevitParameters.ElementLocationEndPoint, ParameterType.Point);
        AddDesc(ref _elementBoundsMin, CommonRevitParameters.ElementBoundsMin, ParameterType.Point);
        AddDesc(ref _elementBoundsMax, CommonRevitParameters.ElementBoundsMax, ParameterType.Point);
        AddDesc(ref _elementAssemblyInstance, CommonRevitParameters.ElementAssemblyInstance, ParameterType.Entity);
        AddDesc(ref _elementDesignOption, CommonRevitParameters.ElementDesignOption, ParameterType.Entity);
        AddDesc(ref _elementGroup, CommonRevitParameters.ElementGroup, ParameterType.Entity);
        AddDesc(ref _elementWorkset, CommonRevitParameters.ElementWorkset, ParameterType.Int);
        AddDesc(ref _elementCreatedPhase, CommonRevitParameters.ElementCreatedPhase, ParameterType.Entity);
        AddDesc(ref _elementDemolishedPhase, CommonRevitParameters.ElementDemolishedPhase, ParameterType.Entity);
        AddDesc(ref _elementCategory, CommonRevitParameters.ElementCategory, ParameterType.Entity);
        AddDesc(ref _elementIsViewSpecific, CommonRevitParameters.ElementIsViewSpecific, ParameterType.Int);
        AddDesc(ref _elementOwnerView, CommonRevitParameters.ElementOwnerView, ParameterType.Entity);

        AddDesc(ref _familyInstanceToRoomDesc, CommonRevitParameters.FIToRoom, ParameterType.Entity);
        AddDesc(ref _familyInstanceFromRoomDesc, CommonRevitParameters.FIFromRoom, ParameterType.Entity);
        AddDesc(ref _familyInstanceHost, CommonRevitParameters.FIHost, ParameterType.Entity);
        AddDesc(ref _familyInstanceSpace, CommonRevitParameters.FISpace, ParameterType.Entity);
        AddDesc(ref _familyInstanceRoom, CommonRevitParameters.FIRoom, ParameterType.Entity);
        AddDesc(ref _familyInstanceFamilyType, CommonRevitParameters.FIFamilyType, ParameterType.Entity);
        AddDesc(ref _familyInstanceStructuralUsage, CommonRevitParameters.FIStructuralUsage, ParameterType.String);
        AddDesc(ref _familyInstanceStructuralMaterialType, CommonRevitParameters.FIStructuralMaterialType, ParameterType.String);
        AddDesc(ref _familyInstanceStructuralMaterialId, CommonRevitParameters.FIStructuralMaterialId, ParameterType.Entity);
        AddDesc(ref _familyInstanceStructuralType, CommonRevitParameters.FIStructuralType, ParameterType.String);

        AddDesc(ref _familyStructuralCodeName, CommonRevitParameters.FamilyStructuralCodeName, ParameterType.String);
        AddDesc(ref _familyStructuralMaterialType, CommonRevitParameters.FamilyStructuralMaterialType, ParameterType.String);

        AddDesc(ref _roomNumber, CommonRevitParameters.RoomNumber, ParameterType.String);
        AddDesc(ref _roomBaseOffset, CommonRevitParameters.RoomBaseOffset, ParameterType.Double);
        AddDesc(ref _roomLimitOffset, CommonRevitParameters.RoomLimitOffset, ParameterType.Double);
        AddDesc(ref _roomUnboundedHeight, CommonRevitParameters.RoomUnboundedHeight, ParameterType.Double);
        AddDesc(ref _roomVolume, CommonRevitParameters.RoomVolume, ParameterType.Double);
        AddDesc(ref _roomUpperLimit, CommonRevitParameters.RoomUpperLimit, ParameterType.Entity);

        AddDesc(ref _levelProjectElevation, CommonRevitParameters.LevelProjectElevation, ParameterType.Double);
        AddDesc(ref _levelElevation, CommonRevitParameters.LevelElevation, ParameterType.Double);

        AddDesc(ref _materialColorRed, CommonRevitParameters.MaterialColorRed, ParameterType.Double);
        AddDesc(ref _materialColorGreen, CommonRevitParameters.MaterialColorGreen, ParameterType.Double);
        AddDesc(ref _materialColorBlue, CommonRevitParameters.MaterialColorBlue, ParameterType.Double);
        AddDesc(ref _materialShininess, CommonRevitParameters.MaterialShininess, ParameterType.Double);
        AddDesc(ref _materialSmoothness, CommonRevitParameters.MaterialSmoothness, ParameterType.Double);
        AddDesc(ref _materialCategory, CommonRevitParameters.MaterialCategory, ParameterType.String);
        AddDesc(ref _materialClass, CommonRevitParameters.MaterialClass, ParameterType.String);
        AddDesc(ref _materialTransparency, CommonRevitParameters.MaterialTransparency, ParameterType.Double);

        AddDesc(ref _worksetKind, CommonRevitParameters.WorksetKind, ParameterType.String);

        AddDesc(ref _layerIndex, CommonRevitParameters.LayerIndex, ParameterType.Int);
        AddDesc(ref _layerFunction, CommonRevitParameters.LayerFunction, ParameterType.String);
        AddDesc(ref _layerWidth, CommonRevitParameters.LayerWidth, ParameterType.Double);
        AddDesc(ref _layerMaterialId, CommonRevitParameters.LayerMaterialId, ParameterType.Entity);
        AddDesc(ref _layerIsCore, CommonRevitParameters.LayerIsCore, ParameterType.Int);

        AddDesc(ref _documentCreationGuid, CommonRevitParameters.DocumentCreationGuid, ParameterType.String);
        AddDesc(ref _documentWorksharingGuid, CommonRevitParameters.DocumentWorksharingGuid, ParameterType.String);
        AddDesc(ref _documentTitle, CommonRevitParameters.DocumentTitle, ParameterType.String);
        AddDesc(ref _documentPath, CommonRevitParameters.DocumentPath, ParameterType.String);
        AddDesc(ref _documentElevation, CommonRevitParameters.DocumentElevation, ParameterType.Double);
        AddDesc(ref _documentLatitude, CommonRevitParameters.DocumentLatitude, ParameterType.Double);
        AddDesc(ref _documentLongitude, CommonRevitParameters.DocumentLongitude, ParameterType.Double);
        AddDesc(ref _documentPlaceName, CommonRevitParameters.DocumentPlaceName, ParameterType.String);
        AddDesc(ref _documentWeatherStationName, CommonRevitParameters.DocumentWeatherStationName, ParameterType.String);
        AddDesc(ref _documentTimeZone, CommonRevitParameters.DocumentTimeZone, ParameterType.String);

        AddDesc(ref _documentLastSaveTime, CommonRevitParameters.DocumentLastSaveTime, ParameterType.String);
        AddDesc(ref _documentSaveCount, CommonRevitParameters.DocumentSaveCount, ParameterType.Int);
        AddDesc(ref _documentIsDetached, CommonRevitParameters.DocumentIsDetached, ParameterType.Int);
        AddDesc(ref _documentIsLinked, CommonRevitParameters.DocumentIsLinked, ParameterType.Int);

        AddDesc(ref _projectName, CommonRevitParameters.ProjectName, ParameterType.String);
        AddDesc(ref _projectNumber, CommonRevitParameters.ProjectNumber, ParameterType.String);
        AddDesc(ref _projectStatus, CommonRevitParameters.ProjectStatus, ParameterType.String);
        AddDesc(ref _projectAddress, CommonRevitParameters.ProjectAddress, ParameterType.String);
        AddDesc(ref _projectClientName, CommonRevitParameters.ProjectClientName, ParameterType.String);
        AddDesc(ref _projectIssueDate, CommonRevitParameters.ProjectIssueDate, ParameterType.String);
        AddDesc(ref _projectAuthor, CommonRevitParameters.ProjectAuthor, ParameterType.String);
        AddDesc(ref _projectBuildingName, CommonRevitParameters.ProjectBuildingName, ParameterType.String);
        AddDesc(ref _projectOrgDescription, CommonRevitParameters.ProjectOrgDescription, ParameterType.String);
        AddDesc(ref _projectOrgName, CommonRevitParameters.ProjectOrgName, ParameterType.String);

        AddDesc(ref _categoryCategoryType, CommonRevitParameters.CategoryCategoryType, ParameterType.String);
        AddDesc(ref _categoryBuiltInType, CommonRevitParameters.CategoryBuiltInType, ParameterType.String);
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

    public void AddParameter(EntityIndex ei, DescriptorIndex di, string val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, DateTime val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        var str = val.ToString("o", CultureInfo.InvariantCulture);
        Builder.AddParameter(ei, str, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, EntityIndex val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Entity) throw new Exception($"Expected entity not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, PointIndex val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Point) throw new Exception($"Expected point not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, int val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, bool val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val ? 1 : 0, di);
    }

    public void AddParameter(EntityIndex ei, DescriptorIndex di, double val)
    {
        var d = Builder.Data.Get(di);
        if (d.Type != ParameterType.Double) throw new Exception($"Expected double not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddTypeAsParameter(EntityIndex ei, object o)
    {
        AddParameter(ei, _apiTypeDescriptor, o.GetType().Name);
    }

    public EntityIndex ProcessCategory(Category category)
    {
        if (!ProcessedCategories.TryGetValue(category.Id.Value, out var result))
            return result;

        var r = Builder.AddEntity(category.Id.Value, category.Id.ToString(), CurrentDocumentIndex, category.Name,
            category.BuiltInCategory.ToString());

        AddParameter(r, _apiTypeDescriptor, category.GetType().Name);
        AddParameter(r, _categoryCategoryType, category.CategoryType.ToString());
        AddParameter(r, _categoryCategoryType, category.BuiltInCategory.ToString());

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

            AddParameter(layerEi, _layerIndex, index);
            AddParameter(layerEi, _layerFunction, layer.Function.ToString());
            AddParameter(layerEi, _layerWidth, layer.Width);
            AddParameter(layerEi, _layerIsCore, layer.IsCore ? 1 : 0);

            var matId = layer.MaterialId;
            if (matId != ElementId.InvalidElementId)
            {
                var matIndex = ProcessElement(matId);
                AddParameter(layerEi, _layerMaterialId, matIndex);
                Builder.AddRelation(layerEi, matIndex, RelationType.HasMaterial);
            }

            AddTypeAsParameter(layerEi, layer);
            Builder.AddRelation(ei, layerEi, RelationType.HasLayer);
        }
    }

    public void ProcessMaterial(EntityIndex ei, Material m)
    {
        var color = m.Color;            
        AddParameter(ei, _materialColorGreen, color.Red);
        AddParameter(ei, _materialColorGreen, color.Green);
        AddParameter(ei, _materialColorGreen, color.Blue);

        AddParameter(ei, _materialTransparency, m.Transparency);
        AddParameter(ei, _materialShininess, m.Shininess);
        AddParameter(ei, _materialSmoothness, m.Smoothness);
        AddParameter(ei, _materialCategory, m.MaterialCategory);
        AddParameter(ei, _materialClass, m.MaterialClass);
    }

    public void ProcessFamily(EntityIndex ei, Family f)
    {
        AddParameter(ei, _familyStructuralCodeName, f.StructuralCodeName);
        AddParameter(ei, _familyStructuralMaterialType, f.StructuralMaterialType.ToString());
    }
    
    public void ProcessFamilyInstance(EntityIndex ei, FamilyInstance f)
    {
        var typeId = f.GetTypeId();
        if (typeId != ElementId.InvalidElementId)
        {
            var type = ProcessElement(typeId);
            AddParameter(ei, _familyInstanceFamilyType, type);
            Builder.AddRelation(ei, type, RelationType.InstanceOf);
        }

        var toRoom = f.ToRoom;
        if (toRoom != null && toRoom.IsValidObject)
            AddParameter(ei, _familyInstanceToRoomDesc, ProcessElement(toRoom));

        var fromRoom = f.FromRoom;
        if (fromRoom != null && fromRoom.IsValidObject)
            AddParameter(ei, _familyInstanceFromRoomDesc, ProcessElement(fromRoom));

        var host = f.Host;
        if (host != null && host.IsValidObject)
        {
            var hostIndex = ProcessElement(host);
            AddParameter(ei, _familyInstanceHost, hostIndex);
            Builder.AddRelation(ei, hostIndex, RelationType.HostedBy);
        }

        var space = f.Space;
        if (space != null && space.IsValidObject)
        {
            var spaceIndex = ProcessElement(space);
            AddParameter(ei, _familyInstanceSpace, spaceIndex);
            Builder.AddRelation(ei, spaceIndex, RelationType.ContainedIn);
        }

        var room = f.Room;
        if (room != null && room.IsValidObject)
        {
            var roomIndex = ProcessElement(room);
            AddParameter(ei, _familyInstanceRoom, roomIndex);
            Builder.AddRelation(ei, roomIndex, RelationType.ContainedIn);
        }

        var matId = f.StructuralMaterialId;
        if (matId != ElementId.InvalidElementId)
        {
            var matIndex = ProcessElement(matId);
            AddParameter(ei, _familyInstanceStructuralMaterialId, matIndex);
            Builder.AddRelation(ei, matIndex, RelationType.HasMaterial);
        }

        AddParameter(ei, _familyInstanceStructuralUsage, f.StructuralUsage.ToString());
        AddParameter(ei, _familyInstanceStructuralType, f.StructuralMaterialType.ToString());
    }

    public void ProcessRoom(EntityIndex ei, Room room)
    {
        AddParameter(ei, _roomBaseOffset, room.BaseOffset);
        AddParameter(ei, _roomLimitOffset, room.LimitOffset);
        AddParameter(ei, _roomNumber, room.Number);
        AddParameter(ei, _roomUnboundedHeight, room.UnboundedHeight);
        if (room.UpperLimit != null && room.UpperLimit.IsValidObject)
            AddParameter(ei, _roomUpperLimit, ProcessElement(room.UpperLimit));
        AddParameter(ei, _roomVolume, room.Volume);
    }
    
    public void ProcessLevel(EntityIndex ei, Level level)
    {
        AddParameter(ei, _levelElevation, level.Elevation);
        AddParameter(ei, _levelProjectElevation, level.ProjectElevation);
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
    
    public static string GetUnitLabel(Parameter p)
    {
        var spec = p.Definition.GetDataType();
        if (!UnitUtils.IsMeasurableSpec(spec))
            return "";
        var unitId = p.GetUnitTypeId();
        return UnitUtils.GetTypeCatalogStringForUnit(unitId);
    }

    public void ProcessParameters(EntityIndex entityIndex, Element element)
    {
        foreach (Parameter p in element.Parameters)
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

        var eType = e.GetType().Name;
        AddParameter(entityIndex, _apiTypeDescriptor, eType);

        if (category != null && category.IsValid)
        {
            var catIndex = ProcessCategory(category);
            AddParameter(entityIndex, _elementCategory, catIndex);
            Builder.AddRelation(entityIndex, catIndex, RelationType.ContainedIn);
        }

        ProcessParameters(entityIndex, e);
        ProcessMaterials(entityIndex, e);

        var bounds = GetBoundingBoxMinMax(e);
        if (bounds.HasValue)
        {
            var min = AddPoint(Builder, bounds.Value.min);
            var max = AddPoint(Builder, bounds.Value.max);
            AddParameter(entityIndex, _elementBoundsMin, min);
            AddParameter(entityIndex, _elementBoundsMax, max);
        }

        var levelId = e.LevelId;
        if (levelId != ElementId.InvalidElementId)
        {
            var levelIndex = ProcessElement(levelId);
            AddParameter(entityIndex, _elementLevel, levelIndex);
            Builder.AddRelation(entityIndex, levelIndex, RelationType.ContainedIn);
        }

        var assemblyInstanceId = e.AssemblyInstanceId;
        if (assemblyInstanceId != ElementId.InvalidElementId)
        {
            var assemblyIndex = ProcessElement(assemblyInstanceId);
            AddParameter(entityIndex, _elementAssemblyInstance, assemblyIndex);
            Builder.AddRelation(entityIndex, assemblyIndex, RelationType.PartOf);
        }
        
        var location = e.Location;
        if (location != null)
        {
            if (location is LocationPoint lp)
            {
                AddParameter(entityIndex, _elementLocation, AddPoint(Builder, lp.Point));
            }

            if (location is LocationCurve lc)
            {
                if (TryGetLocationEndpoints(lc, out var startPoint, out var endPoint))
                {
                    AddParameter(entityIndex, _elementLocationStartPoint, AddPoint(Builder, startPoint));
                    AddParameter(entityIndex, _elementLocationEndPoint, AddPoint(Builder, endPoint));
                }
            }
        }

        if (e.CreatedPhaseId != ElementId.InvalidElementId)
        {
            var createdPhase = ProcessElement(e.CreatedPhaseId);
            AddParameter(entityIndex, _elementCreatedPhase, createdPhase);
        }

        if (e.DemolishedPhaseId != ElementId.InvalidElementId)
        {
            var demolishedPhase = ProcessElement(e.DemolishedPhaseId);
            AddParameter(entityIndex, _elementDemolishedPhase, demolishedPhase);
        }

        var designOption = e.DesignOption;
        if (designOption != null && designOption.IsValidObject)
        {
            var doIndex = ProcessElement(designOption);
            Builder.AddRelation(entityIndex, doIndex, RelationType.MemberOf);
            AddParameter(entityIndex, _elementDesignOption, doIndex);
        }

        var groupId = e.GroupId;
        if (groupId != ElementId.InvalidElementId)
        {
            var group = ProcessElement(groupId);
            Builder.AddRelation(entityIndex, group, RelationType.MemberOf);
            AddParameter(entityIndex, _elementGroup, group);
        }

        if (e.WorksetId != null)
        {
            AddParameter(entityIndex, _elementWorkset, e.WorksetId.IntegerValue);
        }

        if (e.ViewSpecific)
        {
            AddParameter(entityIndex, _elementIsViewSpecific, 1);
        }

        if (e.OwnerViewId != ElementId.InvalidElementId)
        {
            var view = ProcessElement(e.OwnerViewId);
            AddParameter(entityIndex, _elementOwnerView, view);
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

        // TODO: handle Mechanica; space
        //if (e is Space space)
           
        // TODO: let's handle schedules. 

        // TODO: consider adjacency, and fixtures. 

        // TODO: look at connected systems as well. 
        // TODO: phases
        // TODO: views 
        // TODO: fixtures
        // TODO: bounding walls 

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
            AddParameter(ei, _documentLastSaveTime, saveDate);
            var fileInfo = BasicFileInfo.Extract(d.PathName);
            var docVersion = fileInfo.GetDocumentVersion();
            if (docVersion != null)
            {
                AddParameter(ei, _documentSaveCount, docVersion.NumberOfSaves);
            }
        }

        AddParameter(ei, _documentPath, CurrentDocument.PathName);
        AddParameter(ei, _documentTitle, CurrentDocument.Title);
        AddParameter(ei, _documentIsDetached, CurrentDocument.IsDetached ? 1 : 0);
        AddParameter(ei, _documentIsLinked, CurrentDocument.IsLinked ? 1 : 0);

        // TODO: what about "IsDetached"?

        if (CurrentDocument.IsWorkshared)
            AddParameter(ei, _documentWorksharingGuid, CurrentDocument.WorksharingCentralGUID.ToString());
        AddParameter(ei, _documentCreationGuid, CurrentDocument.CreationGUID.ToString());
        AddParameter(ei, _documentElevation, siteLocation.Elevation);
        AddParameter(ei, _documentLatitude, siteLocation.Latitude);
        AddParameter(ei, _documentLongitude, siteLocation.Longitude);
        AddParameter(ei, _documentPlaceName, siteLocation.PlaceName);
        AddParameter(ei, _documentWeatherStationName, siteLocation.WeatherStationName);
        AddParameter(ei, _documentTimeZone, siteLocation.TimeZone);

        var projectInfo = CurrentDocument.ProjectInformation;

        AddParameter(ei, _projectAddress, projectInfo.Address);
        AddParameter(ei, _projectAuthor, projectInfo.Author);
        AddParameter(ei, _projectBuildingName, projectInfo.BuildingName);
        AddParameter(ei, _projectClientName, projectInfo.ClientName);
        AddParameter(ei, _projectIssueDate, projectInfo.IssueDate);
        AddParameter(ei, _projectName, projectInfo.Name);
        AddParameter(ei, _projectNumber, projectInfo.Number);
        AddParameter(ei, _projectOrgDescription, projectInfo.OrganizationDescription);
        AddParameter(ei, _projectOrgName, projectInfo.OrganizationName);
        AddParameter(ei, _projectStatus, projectInfo.Status);

        foreach (var e in CurrentDocument.GetElements())
            ProcessElement(e);

        if (IncludeLinks)
            foreach (var linkedDoc in d.GetLinkedDocuments())
                ProcessDocument(linkedDoc);
    }
}