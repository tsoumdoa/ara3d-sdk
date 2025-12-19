using Ara3D.BimOpenSchema;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static Ara3D.BimOpenSchema.CommonRevitParameters;
using Document = Autodesk.Revit.DB.Document;
using Domain = Autodesk.Revit.DB.Domain;
using Exception = System.Exception;
using Parameter = Ara3D.BimOpenSchema.Parameter;
using RevitParameter = Autodesk.Revit.DB.Parameter;

namespace Ara3D.Bowerbird.RevitSamples;

public readonly record struct ElementKey(
    int DocKey, 
    long ElementId
);

public class BimOpenSchemaRevitBuilder
{
    public BimOpenSchemaRevitBuilder(Document rootDocument, bool includeLinks, bool processDoc = true) 
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
    public int CurrentDocumentKey;
    public Dictionary<string, DescriptorIndex> DescriptorLookup = new();
    public Dictionary<ElementKey, EntityIndex> ProcessedEntities = new();
    public Dictionary<int, DocumentIndex> ProcessedDocuments = new();
    public Dictionary<int, Dictionary<int, EntityIndex>> ProcessedConnectors = new();
    public Dictionary<long, EntityIndex> ProcessedCategories = new();
    public int BoundaryCount;

    public EntityIndex GetEntityIndex(Document doc, long entityId)
        => GetEntityIndex(GetDocumentKey(doc), entityId);

    public EntityIndex GetEntityIndex(int key, long entityId)
        => ProcessedEntities[GetElementKey(key, entityId)];

    public EntityIndex GetEntityIndex(ElementKey key)
        => ProcessedEntities[key];

    public static (XYZ min, XYZ max)? GetBoundingBoxMinMax(Element element, View view = null)
    {
        if (element == null) return null;
        var bb = element.get_BoundingBox(view);
        return bb == null ? null : (bb.Min, bb.Max);
    }

    public PointIndex AddPoint(XYZ xyz)
        => AddPoint(Builder, xyz);

    public static PointIndex AddPoint(BimDataBuilder bdb, XYZ xyz)
        => bdb.AddPoint(new((float)xyz.X, (float)xyz.Y, (float)xyz.Z));

    public void CreateCommonDescriptors()
    {
        foreach (var p in GetParameters())
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
    
    public void AddParameter(EntityIndex ei, Parameter p, Func<XYZ> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, XYZ xyz)
        => AddParameter(ei, DescriptorLookup[p.Name], AddPoint(xyz));

    public void AddParameter(EntityIndex ei, Parameter p, Func<string> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, string val)
        => AddParameter(ei, DescriptorLookup[p.Name], val ?? "");

    public void AddParameter(EntityIndex ei, DescriptorIndex di, string val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, Func<DateTime> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, DateTime val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, DateTime val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.String) throw new Exception($"Expected string not {d.Type}");
        var str = val.ToString("o", CultureInfo.InvariantCulture);
        Builder.AddParameter(ei, str, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, ElementId eId, RelationType rt)
    {
        if (eId != ElementId.InvalidElementId)
        {
            var ei2 = ProcessElement(eId);
            AddParameter(ei, DescriptorLookup[p.Name], ei2);
            Builder.AddRelation(ei, ei2, rt);
        }
    }

    public void AddParameter(EntityIndex ei, Parameter p, Element e, RelationType rt)
    {
        if (e != null && e.IsValidObject)
        {
            var ei2 = ProcessElement(e);
            AddParameter(ei, DescriptorLookup[p.Name], ei2);
            Builder.AddRelation(ei, ei2, rt);
        }
    }

    public void AddParameter(EntityIndex ei, Parameter p, ElementId eId)
    {
        if (eId != ElementId.InvalidElementId)
            AddParameter(ei, DescriptorLookup[p.Name], ProcessElement(eId));
    }

    public void AddParameter(EntityIndex ei, Parameter p, Element e)
    {
        if (e != null && e.IsValidObject) 
            AddParameter(ei, DescriptorLookup[p.Name], ProcessElement(e));
    }

    public void AddParameter(EntityIndex ei, Parameter p, EntityIndex val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, EntityIndex val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.Entity) throw new Exception($"Expected entity not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, PointIndex val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, PointIndex val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.Point) throw new Exception($"Expected point not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, Func<int> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, int val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, int val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, Func<bool> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, bool val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, bool val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.Int) throw new Exception($"Expected int not {d.Type}");
        Builder.AddParameter(ei, val ? 1 : 0, di);
    }

    public void AddParameter(EntityIndex ei, Parameter p, Func<double> f)
    { try { AddParameter(ei, p, f()); } catch { } }

    public void AddParameter(EntityIndex ei, Parameter p, double val)
        => AddParameter(ei, DescriptorLookup[p.Name], val);

    public void AddParameter(EntityIndex ei, DescriptorIndex di, double val)
    {
        var d = Builder.Get(di);
        if (d.Type != ParameterType.Number) throw new Exception($"Expected double not {d.Type}");
        Builder.AddParameter(ei, val, di);
    }

    public void AddTypeAsParameter(EntityIndex ei, object o)
        => AddTypeAsParameter(ei, o.GetType().Name);

    public void AddTypeAsParameter(EntityIndex ei, string typeName)
        => AddParameter(ei, ObjectTypeName, typeName);

    public void AddErrorAsParameter(EntityIndex ei, Exception ex)
        => AddParameter(ei, ObjectError, ex.Message);

    public EntityIndex ProcessCategory(Category category)
    {
        if (!ProcessedCategories.TryGetValue(category.Id.Value, out var result))
            return result;

        var r = Builder.AddEntity(
            category.Id.Value, 
            category.Id.ToString(), 
            CurrentDocumentIndex, 
            category.Name,
            category.BuiltInCategory.ToString());

        try
        {
            AddTypeAsParameter(r, category);
            AddParameter(r, CategoryCategoryType, category.CategoryType.ToString());
            AddParameter(r, CategoryBuiltInType, category.BuiltInCategory.ToString());

            foreach (Category subCategory in category.SubCategories)
            {
                var subCatId = ProcessCategory(subCategory);
                Builder.AddRelation(subCatId, r, RelationType.ChildOf);
            }
        }
        catch (Exception ex)
        {
            AddErrorAsParameter(r, ex);
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
            AddParameter(layerEi, LayerMaterialId, layer.MaterialId, RelationType.HasMaterial);

            AddTypeAsParameter(layerEi, layer);
            Builder.AddRelation(ei, layerEi, RelationType.HasLayer);
        }
    }

    public void ProcessTextNote(EntityIndex ei, TextNote tn)
    {
        AddParameter(ei, TextNoteCoord, tn.Coord);
        AddParameter(ei, TextNoteDir, tn.BaseDirection);
        AddParameter(ei, TextNoteHeight, tn.Height);
        AddParameter(ei, TextNoteWidth, tn.Width);
        AddParameter(ei, TextNoteText, tn.Text);
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
        AddParameter(ei, FIFamilyType, f.GetTypeId(), RelationType.InstanceOf);
        AddParameter(ei, FIToRoom, f.ToRoom);
        AddParameter(ei, FIFromRoom, f.FromRoom);
        AddParameter(ei, FIHost, f.Host, RelationType.HostedBy);
        AddParameter(ei, FISpace, f.Space, RelationType.ContainedIn);
        AddParameter(ei, FIRoom, f.Room, RelationType.ContainedIn);
        AddParameter(ei, FIStructuralMaterial, f.StructuralMaterialId, RelationType.HasMaterial);
        AddParameter(ei, FIStructuralUsage, f.StructuralUsage.ToString());
        AddParameter(ei, FIStructuralMaterialType, f.StructuralMaterialType.ToString());
        AddParameter(ei, FIStructuralType, f.StructuralType.ToString());
    }

    public Element GetElement(Document d, ElementId id, ElementId linkedElementId)
    {
        if (linkedElementId == ElementId.InvalidElementId)
            return d.GetElement(id);

        var linkInstance = d.GetElement(id) as RevitLinkInstance;
        return linkInstance?.GetLinkDocument()?
            .GetElement(linkedElementId);
    }

    // Boundaries are pseudo-entities. 
    public void ProcessBoundary(Document d, EntityIndex roomEntityIndex, BoundarySegment bs, bool isOuterBoundary)
    {
        var boundaryElement = GetElement(d, bs.ElementId, bs.LinkElementId);
        if (boundaryElement == null)
            return;

        var boundaryElementEntityIndex = ProcessElement(boundaryElement);

        var name = boundaryElement.Name;
        var newEi = Builder.AddEntity(
            BoundaryCount++,
            "",
            CurrentDocumentIndex,
            name,
            BoundaryEntityName);

        AddParameter(newEi, BoundaryOuter, isOuterBoundary);
        AddParameter(newEi, BoundaryElement, boundaryElementEntityIndex);
        Builder.AddRelation(roomEntityIndex, boundaryElementEntityIndex, RelationType.BoundedBy);
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
            try 
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
                        AddParameter(entityIndex, Builder.AddDescriptor(def.Name, unitLabel, groupLabel, ParameterType.Number), p.AsDouble());
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
            catch (Exception ex) 
            {
                AddErrorAsParameter(entityIndex, ex);
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

    public static ConnectorManager TryGetConnectorManager(Element e)
    {
        switch (e)
        {
            case MEPCurve mepCurve:
                return mepCurve.ConnectorManager;

            case FabricationPart fab:
                return fab.ConnectorManager;

            case FamilyInstance fi:
                return fi.MEPModel?.ConnectorManager;

            default:
                return null;
        }
    }

    public static IEnumerable<ConnectorManager> GetConnectorManagers(Document doc)
    {
        // FamilyInstances with MEPModel
        foreach (var fi in doc.GetElements<FamilyInstance>())
        {
            var cm = fi.MEPModel?.ConnectorManager;
            if (cm != null)
                yield return cm;
        }

        // All MEPCurves (Pipes, Ducts, etc.)
        foreach (var curve in doc.GetElements<MEPCurve>())
        {
            var cm = curve.ConnectorManager;
            if (cm != null)
                yield return cm;
        }

        // Fabrication parts, if you're using them
        foreach (var fab in doc.GetElements<FabricationPart>())
        {
            var cm = fab.ConnectorManager;
            if (cm != null)
                yield return cm;
        }
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
        var docKey = GetDocumentKey(element.Document);
        var key = GetElementKey(docKey, element.Id.Value);
        if (ProcessedEntities.TryGetValue(key, out var found))
            return found;
        return ProcessNewElement(key, element);
    }

    public void AddParameter(EntityIndex entityIndex, Parameter p, Connector c)
    {
        if (c == null || p == null)
            return;
        if (!ProcessedConnectors.TryGetValue(CurrentDocumentKey, out var d))
            return;
        if (!d.TryGetValue(c.Id, out var connectorEntityIndex))
            return;
        AddParameter(entityIndex, p, connectorEntityIndex);
    }

    public EntityIndex ProcessNewElement(ElementKey key, Element e)
    {
        var category = e.Category;
        var catName = (category != null && category.IsValid) ? category.Name : "";

        var ei = Builder.AddEntity(e.Id.Value, e.UniqueId, CurrentDocumentIndex, e.Name, catName);
        ProcessedEntities.Add(key, ei);

        AddTypeAsParameter(ei, e);

        if (category != null && category.IsValid)
        {
            var catIndex = ProcessCategory(category);
            AddParameter(ei, ObjectCategory, catIndex);
            Builder.AddRelation(ei, catIndex, RelationType.MemberOf);
        }

        ProcessParameters(ei, e);
        ProcessMaterials(ei, e);

        var bounds = GetBoundingBoxMinMax(e);
        if (bounds.HasValue)
        {
            AddParameter(ei, ElementBoundsMin, bounds.Value.min);
            AddParameter(ei, ElementBoundsMax, bounds.Value.max);
        }

        AddParameter(ei, ElementLevel, e.LevelId, RelationType.ContainedIn);
        AddParameter(ei, ElementAssemblyInstance, e.AssemblyInstanceId, RelationType.PartOf);

        var location = e.Location;
        if (location != null)
        {
            if (location is LocationPoint lp)
            {
                AddParameter(ei, ElementLocationPoint, AddPoint(Builder, lp.Point));
            }

            if (location is LocationCurve lc)
            {
                if (TryGetLocationEndpoints(lc, out var startPoint, out var endPoint))
                {
                    AddParameter(ei, ElementLocationStartPoint, AddPoint(Builder, startPoint));
                    AddParameter(ei, ElementLocationEndPoint, AddPoint(Builder, endPoint));
                }
            }
        }

        AddParameter(ei, ElementCreatedPhase, e.CreatedPhaseId);
        AddParameter(ei, ElementDemolishedPhase, e.DemolishedPhaseId);
        AddParameter(ei, ElementDesignOption, e.DesignOption, RelationType.MemberOf);
        AddParameter(ei, ElementGroup, e.GroupId, RelationType.MemberOf);

        if (e.Document.IsWorkshared)
            AddParameter(ei, ElementWorksetId, e.WorksetId.IntegerValue);

        if (e.ViewSpecific)
            AddParameter(ei, ElementIsViewSpecific, true);

        AddParameter(ei, ElementOwnerView, e.OwnerViewId);

        TryProcessAs<HostObjAttributes>(e, ei, ProcessCompoundStructure);
        TryProcessAs<Level>(e, ei, ProcessLevel);
        TryProcessAs<Family>(e, ei, ProcessFamily);
        TryProcessAs<FamilyInstance>(e, ei, ProcessFamilyInstance);
        TryProcessAs<Material>(e, ei, ProcessMaterial);
        TryProcessAs<TextNote>(e, ei, ProcessTextNote);
        TryProcessAs<SpatialElement>(e, ei, ProcessSpatialElement);
        TryProcessAs<MEPSystem>(e, ei, ProcessMepSystem);
        TryProcessAs<Zone>(e, ei, ProcessZone);

        return ei;
    }

    public void TryProcessAs<T>(Element e, EntityIndex entityIndex, Action<EntityIndex, T> processor)
        where T: Element
    {
        if (e is T val)
        {
            try
            {
                processor(entityIndex, val);
            }
            catch (Exception ex)
            {
                AddErrorAsParameter(entityIndex, ex);
            }
        }
    }

    public void ProcessSpatialElement(EntityIndex ei, SpatialElement se)
    {

        TryProcessAs<Space>(se, ei, ProcessSpace);
        TryProcessAs<Area>(se, ei, ProcessArea);
        TryProcessAs<Room>(se, ei, ProcessRoom);

        var options = new SpatialElementBoundaryOptions();
        IList<IList<BoundarySegment>> segmentLists = se.GetBoundarySegments(options);

        var doc = se.Document;
        for (var i = 0; i < segmentLists.Count; i++)
        {
            var segmentList = segmentLists[i];
            foreach (var segment in segmentList)
            {
                ProcessBoundary(doc, ei, segment, i == 0);
            }
        }
    }

    public void ProcessArea(EntityIndex ei, Area area)
    {
        AddParameter(ei, AreaSchemeParameter, area.AreaScheme);
        AddParameter(ei, AreaIsGrossInterior, area.IsGrossInterior);
    }

    public void ProcessRoom(EntityIndex ei, Room room)
    {
        AddParameter(ei, RoomBaseOffset, room.BaseOffset);
        AddParameter(ei, RoomLimitOffset, room.LimitOffset);
        AddParameter(ei, RoomNumber, room.Number);
        AddParameter(ei, RoomUnboundedHeight, room.UnboundedHeight);
        AddParameter(ei, RoomUpperLimit, room.UpperLimit);
        AddParameter(ei, RoomVolume, room.Volume);
    }

    public void ProcessSpace(EntityIndex ei, Space space)
    {
        // Actual loads / airflows
        AddParameter(ei, SpaceActualExhaustAirflow, space.ActualExhaustAirflow);
        AddParameter(ei, SpaceActualHVACLoad, space.ActualHVACLoad);
        AddParameter(ei, SpaceActualLightingLoad, space.ActualLightingLoad);
        AddParameter(ei, SpaceActualOtherLoad, space.ActualOtherLoad);
        AddParameter(ei, SpaceActualPowerLoad, space.ActualPowerLoad);
        AddParameter(ei, SpaceActualReturnAirflow, space.ActualReturnAirflow);
        AddParameter(ei, SpaceActualSupplyAirflow, space.ActualSupplyAirflow);

        // Air changes / people / illumination
        AddParameter(ei, SpaceAirChangesPerHour, space.AirChangesPerHour);
        AddParameter(ei, SpaceAreaPerPerson, space.AreaperPerson);
        AddParameter(ei, SpaceAverageEstimatedIllumination, space.AverageEstimatedIllumination);

        // Base / limit / height
        AddParameter(ei, SpaceBaseHeatLoadOn, space.BaseHeatLoadOn.ToString());
        AddParameter(ei, SpaceBaseOffset, space.BaseOffset);
        AddParameter(ei, SpaceLimitOffset, space.LimitOffset);
        AddParameter(ei, SpaceUnboundedHeight, space.UnboundedHeight);

        // Calculated loads / flows
        // NOTE: may or may not be computed. 
        AddParameter(ei, SpaceCalculatedCoolingLoad, () => space.CalculatedCoolingLoad);
        AddParameter(ei, SpaceCalculatedHeatingLoad, () => space.CalculatedHeatingLoad);
        AddParameter(ei, SpaceCalculatedSupplyAirflow, () => space.CalculatedSupplyAirflow);

        // Reflectances
        AddParameter(ei, SpaceCeilingReflectance, space.CeilingReflectance);
        AddParameter(ei, SpaceFloorReflectance, space.FloorReflectance);
        AddParameter(ei, SpaceWallReflectance, space.WallReflectance);

        // Condition / units / type (enums → string)
        AddParameter(ei, SpaceConditionType, space.ConditionType.ToString());
        AddParameter(ei, SpaceLightingLoadUnit, space.LightingLoadUnit.ToString());
        AddParameter(ei, SpacePowerLoadUnit, space.PowerLoadUnit.ToString());
        AddParameter(ei, SpaceOccupancyUnit, space.OccupancyUnit.ToString());
        AddParameter(ei, SpaceOutdoorAirFlowStandard, space.OutdoorAirFlowStandard.ToString());
        AddParameter(ei, SpaceSpaceType, space.SpaceType.ToString());

        // Design loads / flows
        AddParameter(ei, SpaceDesignCoolingLoad, space.DesignCoolingLoad);
        AddParameter(ei, SpaceDesignExhaustAirflow, space.DesignExhaustAirflow);
        AddParameter(ei, SpaceDesignHeatingLoad, space.DesignHeatingLoad);
        AddParameter(ei, SpaceDesignHVACLoadPerArea, space.DesignHVACLoadperArea);
        AddParameter(ei, SpaceDesignLightingLoad, space.DesignLightingLoad);
        AddParameter(ei, SpaceDesignOtherLoadPerArea, space.DesignOtherLoadperArea);
        AddParameter(ei, SpaceDesignPowerLoad, space.DesignPowerLoad);
        AddParameter(ei, SpaceDesignReturnAirflow, space.DesignReturnAirflow);
        AddParameter(ei, SpaceDesignSupplyAirflow, space.DesignSupplyAirflow);

        // People / gains
        AddParameter(ei, SpaceNumberOfPeople, space.NumberofPeople);
        AddParameter(ei, SpaceLatentHeatGainPerPerson, space.LatentHeatGainperPerson);
        AddParameter(ei, SpaceSensibleHeatGainPerPerson, space.SensibleHeatGainperPerson);

        // Outdoor air / ventilation
        AddParameter(ei, SpaceOutdoorAirflow, space.OutdoorAirflow);
        AddParameter(ei, SpaceOutdoorAirPerArea, space.OutdoorAirPerArea);
        AddParameter(ei, SpaceOutdoorAirPerPerson, space.OutdoorAirPerPerson);

        // Misc numeric
        AddParameter(ei, SpaceLightingCalculationWorkplane, space.LightingCalculationWorkplane);
        AddParameter(ei, SpaceReturnAirflow, space.ReturnAirflow.ToString());
        AddParameter(ei, SpaceSpaceCavityRatio, space.SpaceCavityRatio);
        AddParameter(ei, SpaceVolume, space.Volume);

        // Booleans
        AddParameter(ei, SpaceOccupiable, space.Occupiable);
        AddParameter(ei, SpacePlenum, space.Plenum);

        AddParameter(ei, SpaceUpperLimit, space.UpperLimit);
        AddParameter(ei, SpaceRoom, space.Room);
        AddParameter(ei, SpaceSpaceTypeElement, space.SpaceTypeId);
        AddParameter(ei, SpaceZone, space.Zone);
    }

    public void ProcessMepSystem(EntityIndex ei, MEPSystem sys)
    {
        AddParameter(ei, MepSystemBaseEquipment, sys.BaseEquipment);
        AddParameter(ei, MepSystemBaseEquipmentConnector, sys.BaseEquipmentConnector);
        AddParameter(ei, MepSystemHasDesignParts, sys.HasDesignParts);
        AddParameter(ei, MepSystemHasFabricationParts, sys.HasFabricationParts);
        AddParameter(ei, MepSystemHasPlaceholders, sys.HasPlaceholders);
        AddParameter(ei, MepSystemIsEmpty, sys.IsEmpty);
        AddParameter(ei, MepSystemIsMultipleNetwork, sys.IsMultipleNetwork);
        AddParameter(ei, MepSystemIsValid, sys.IsValid);
        AddParameter(ei, MepSystemSectionsCount, sys.SectionsCount);

        foreach (Element terminal in sys.Elements)
        {
            var terminalEntityIndex = ProcessElement(terminal);
            Builder.AddRelation(terminalEntityIndex, ei, RelationType.MemberOf);
        }

        TryProcessAs<ElectricalSystem>(sys, ei, ProcessElectricalSystem);
        TryProcessAs<MechanicalSystem>(sys, ei, ProcessMechanicalSystem);
        TryProcessAs<PipingSystem>(sys, ei, ProcessPipingSystem);
    }

    public void ProcessMechanicalSystem(EntityIndex ei, MechanicalSystem ms)
    {
        AddParameter(ei, MechSystemType, ms.SystemType.ToString());
        AddParameter(ei, MechSystemIsWellConnected, ms.IsWellConnected);

        foreach (Element duct in ms.DuctNetwork)
        {
            var ductEntityIndex = ProcessElement(duct);
            Builder.AddRelation(ductEntityIndex, ei, RelationType.MemberOf);
        }
    }

    public void ProcessElectricalSystem(EntityIndex ei, ElectricalSystem es)
    {
        AddParameter(ei, ElecSystemType, es.SystemType.ToString());
        AddParameter(ei, ElecSystemBalancedLoad, es.BalancedLoad);
        AddParameter(ei, ElecSystemCircuitConnectionType, es.CircuitConnectionType.ToString());
        AddParameter(ei, ElecSystemCircuitType, es.CircuitType.ToString());
        AddParameter(ei, ElecSystemCircuitNumber, es.CircuitNumber);
        AddParameter(ei, ElecSystemFrame, es.Frame);
        AddParameter(ei, ElecSystemHasCustomCircuitPath, es.HasCustomCircuitPath);
        AddParameter(ei, ElecSystemHotConductorsNumber, es.HotConductorsNumber);
        AddParameter(ei, ElecSystemIsBasePanelFeedThroughLugsOccupied, es.IsBasePanelFeedThroughLugsOccupied);
        AddParameter(ei, ElecSystemLoadClassificationAbbreviations, es.LoadClassificationAbbreviations);
        AddParameter(ei, ElecSystemLoadClassifications, es.LoadClassifications);
        AddParameter(ei, ElecSystemLoadName, es.LoadName);
        AddParameter(ei, ElecSystemNeutralConductorsNumber, es.NeutralConductorsNumber);
        AddParameter(ei, ElecSystemPanelName, es.PanelName);
        AddParameter(ei, ElecSystemPhaseLabel, es.PhaseLabel);
        AddParameter(ei, ElecSystemPolesNumber, es.PolesNumber);
        AddParameter(ei, ElecSystemPowerFactor, es.PowerFactor);
        AddParameter(ei, ElecSystemPowerFactorState, es.PowerFactorState.ToString());
        AddParameter(ei, ElecSystemRating, es.Rating);
        AddParameter(ei, ElecSystemRunsNumber, es.RunsNumber);
        AddParameter(ei, ElecSystemSlotIndex, es.SlotIndex);
        AddParameter(ei, ElecSystemStartSlot, es.StartSlot);
        AddParameter(ei, ElecSystemVoltage, es.Voltage);
        AddParameter(ei, ElecSystemWays, es.Ways);
        AddParameter(ei, ElecSystemWireType, es.WireType.ToString());
        
        AddParameter(ei, ElecSystemLength, () => es.Length);

        // Power-only metrics
        if (es.SystemType == ElectricalSystemType.PowerCircuit)
        {
            AddParameter(ei, ElecSystemApparentCurrent, es.ApparentCurrent);
            AddParameter(ei, ElecSystemApparentLoad, es.ApparentLoad);
            AddParameter(ei, ElecSystemTrueCurrent, es.TrueCurrent);
            AddParameter(ei, ElecSystemTrueLoad, es.TrueLoad);
        }
    }

    public void ProcessPipingSystem(EntityIndex ei, PipingSystem ps)
    {
        var pipesAndFittings = ps.PipingNetwork;

        foreach (Element pipeOrFitting in pipesAndFittings)
        {
            var pipeElementIndex = ProcessElement(pipeOrFitting);
            Builder.AddRelation(pipeElementIndex, ei, RelationType.MemberOf);
        }

        AddParameter(ei, PipingSystemTypeStr, ps.SystemType.ToString());
    }

    public void ProcessZone(EntityIndex entityIndex, Zone z)
    {
        AddParameter(entityIndex, ZoneArea, z.Area);
        AddParameter(entityIndex, ZoneCoolingAirTemperature, z.CoolingAirTemperature);
        AddParameter(entityIndex, ZoneCoolingSetPoint, z.CoolingSetPoint);
        AddParameter(entityIndex, ZoneDehumidificationSetPoint, z.DehumidificationSetPoint);
        AddParameter(entityIndex, ZoneGrossArea, z.GrossArea);
        AddParameter(entityIndex, ZoneGrossVolume, z.GrossVolume);
        AddParameter(entityIndex, ZoneHeatingAirTemperature, z.HeatingAirTemperature);
        AddParameter(entityIndex, ZoneHeatingSetPoint, z.HeatingSetPoint);
        AddParameter(entityIndex, ZonePerimeter, z.Perimeter);
        AddParameter(entityIndex, ZoneServiceType, () => z.ServiceType.ToString());

        var spaces = z.Spaces;
        foreach (Space space in spaces)
        {
            var spaceIndex = ProcessElement(space);
            Builder.AddRelation(entityIndex, spaceIndex, RelationType.ContainedIn);
        }
    }

    public static ElementKey GetElementKey(int docKey, long id)
        => new(docKey, id);

    public static ElementKey GetElementKey(int docKey, ElementId id)
        => GetElementKey(docKey, id.Value);

    public static ElementKey GetElementKey(Document d, ElementId id)
        => GetElementKey(GetDocumentKey(d), id);

    public static int GetDocumentKey(Document d)
        => d.GetHashCode();

    public void ProcessDocument(Document d)
    {
        var key = GetDocumentKey(d);
        if (ProcessedDocuments.ContainsKey(key))
            return;

        CurrentDocument = d;
        CurrentDocumentIndex = Builder.AddDocument(d.Title, d.PathName);
        CurrentDocumentKey = GetDocumentKey(CurrentDocument);

        // NOTE: this creates a pseudo-entity for the document, which is used so that we can associate parameters and meta-data with it. 
        var ei = Builder.AddEntity(ProcessedDocuments.Count, CurrentDocument.CreationGUID.ToString(),
            CurrentDocumentIndex, d.Title, DocumentEntityName);
        ProcessedDocuments.Add(key, CurrentDocumentIndex);

        AddTypeAsParameter(ei, d);

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

        ProcessConnectors();

        try
        {
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
            AddParameter(ei, DocumentTimeZone, siteLocation.TimeZone);

            var projectInfo = CurrentDocument.ProjectInformation;
            if (projectInfo != null)
            {
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
            }
        }
        catch (Exception ex)
        {
            AddErrorAsParameter(ei, ex);
        }

        foreach (var e in CurrentDocument.GetElements())
            ProcessElement(e);

        if (IncludeLinks)
            foreach (var linkedDoc in d.GetLinkedDocuments())
                ProcessDocument(linkedDoc);
    }

    public EntityIndex ProcessConnector(Connector conn)
    {
        // LocalId = conn.Id (int), GlobalId = empty, Name = empty, Category = "Connector"
        var entityIndex = Builder.AddEntity(conn.Id, "", CurrentDocumentIndex, "", ConnectorEntityName);
        AddTypeAsParameter(entityIndex, conn);

        try
        {
            // Cache some things we’ll use a lot
            var domain = conn.Domain;
            var shape = conn.Shape;
            bool ownerIsFamilyInstance = conn.Owner is FamilyInstance;

            // --- Basic identity / classification ---
            AddParameter(entityIndex, ConnectorId, conn.Id.ToString());
            AddParameter(entityIndex, ConnectorTypeStr, conn.ConnectorType.ToString());
            AddParameter(entityIndex, ConnectorShape, shape.ToString());
            AddParameter(entityIndex, ConnectorDomain, domain.ToString());
            AddParameter(entityIndex, ConnectorDescription, conn.Description);
            AddParameter(entityIndex, ConnectorOwner, conn.Owner, RelationType.MemberOf);

            // --- Geometry / location ---
            if (conn.ConnectorType == ConnectorType.Physical)
            {
                AddParameter(entityIndex, ConnectorOrigin, conn.Origin);
                AddParameter(entityIndex, ConnectorCoordinateSystem, conn.CoordinateSystem?.ToString());
                AddParameter(entityIndex, ConnectorIsConnected, conn.IsConnected);
                AddParameter(entityIndex, ConnectorIsMovable, conn.IsMovable);
            }

            // Size / geometry-specific (shape guards)
            switch (shape)
            {
                case ConnectorProfileType.Rectangular:
                case ConnectorProfileType.Oval:
                    // Height / Width only valid for rectangular/oval profiles
                    AddParameter(entityIndex, ConnectorHeight, conn.Height);
                    AddParameter(entityIndex, ConnectorWidth, conn.Width);
                    break;

                case ConnectorProfileType.Round:
                    // Radius only valid for round profiles
                    AddParameter(entityIndex, ConnectorRadius, conn.Radius);
                    break;

                default:
                    // Other shapes exist but have no extra scalar size here
                    break;
            }

            AddParameter(entityIndex, ConnectorEngagementLength, conn.EngagementLength);
            AddParameter(entityIndex, ConnectorGasketLength, conn.GasketLength);

            // --- Flow / performance data (assigned vs actual) ---

            // Assigned (design) values – all of these can throw if:
            //  - connector is not in a family instance, or
            //  - connector is in the wrong domain.
            if (ownerIsFamilyInstance)
            {
                // HVAC-only assigned properties
                if (domain == Domain.DomainHvac)
                {
                    AddParameter(entityIndex, ConnectorAssignedDuctFlowConfiguration,
                        conn.AssignedDuctFlowConfiguration.ToString());
                    AddParameter(entityIndex, ConnectorAssignedDuctLossMethod, conn.AssignedDuctLossMethod.ToString());
                    AddParameter(entityIndex, ConnectorAssignedLossCoefficient, conn.AssignedLossCoefficient);
                }

                // Piping-only assigned properties
                if (domain == Domain.DomainPiping)
                {
                    AddParameter(entityIndex, ConnectorAssignedFixtureUnits, conn.AssignedFixtureUnits);
                    AddParameter(entityIndex, ConnectorAssignedKCoefficient, conn.AssignedKCoefficient);
                    AddParameter(entityIndex, ConnectorAssignedPipeFlowConfiguration,
                        conn.AssignedPipeFlowConfiguration.ToString());
                    AddParameter(entityIndex, ConnectorAssignedPipeLossMethod, conn.AssignedPipeLossMethod.ToString());
                    AddParameter(entityIndex, ConnectorDemand, conn.Demand);
                }

                // Shared between HVAC and Piping
                if (domain == Domain.DomainHvac || domain == Domain.DomainPiping)
                {
                    AddParameter(entityIndex, ConnectorAssignedFlowDirection, conn.AssignedFlowDirection.ToString());
                    AddParameter(entityIndex, ConnectorAssignedFlowFactor, conn.AssignedFlowFactor);
                    AddParameter(entityIndex, ConnectorAssignedPressureDrop, conn.AssignedPressureDrop);
                    AddParameter(entityIndex, ConnectorAssignedFlow, conn.AssignedFlow);
                    AddParameter(entityIndex, ConnectorFlow, conn.Flow);
                    AddParameter(entityIndex, ConnectorPressureDrop, conn.PressureDrop);
                    AddParameter(entityIndex, ConnectorVelocityPressure, conn.VelocityPressure);
                    AddParameter(entityIndex, ConnectorCoefficient, conn.Coefficient);
                    AddParameter(entityIndex, ConnectorDirection, conn.Direction.ToString());
                }
            }

            // --- System-type classification (domain-dependent) ---
            if (domain == Domain.DomainHvac)
            {
                AddParameter(entityIndex, ConnectorDuctSystemType, conn.DuctSystemType.ToString());
            }

            if (domain == Domain.DomainPiping)
            {
                AddParameter(entityIndex, ConnectorPipeSystemType, conn.PipeSystemType.ToString());
            }

            if (domain == Domain.DomainElectrical)
            {
                AddParameter(entityIndex, ConnectorElectricalSystemType, conn.ElectricalSystemType.ToString());
            }

            // Utility (int enum in practice, safe)
            AddParameter(entityIndex, ConnectorUtility, conn.Utility);

            // --- Direction / angle / behavior ---
            if (domain == Domain.DomainHvac || domain == Domain.DomainCableTrayConduit || domain == Domain.DomainPiping)
            {
                AddParameter(entityIndex, ConnectorAngle, conn.Angle);
                AddParameter(entityIndex, ConnectorAllowsSlopeAdjustments, conn.AllowsSlopeAdjustments);
            }
        }
        catch (Exception ex)
        {
            AddErrorAsParameter(entityIndex, ex);
        }

        return entityIndex;
    }


    public void ProcessConnectors()
    {
        var doc = CurrentDocument;
        if (ProcessedConnectors.ContainsKey(CurrentDocumentKey))
            return;

        var currentConnectorSet = new Dictionary<int, EntityIndex>();
        ProcessedConnectors.Add(CurrentDocumentKey, currentConnectorSet);
        
        var cms = GetConnectorManagers(doc);
        foreach (ConnectorManager cm in cms)
        {
            foreach (Connector conn in cm.Connectors)
            {
                if (!currentConnectorSet.ContainsKey(conn.Id))
                {
                    currentConnectorSet.Add(conn.Id, ProcessConnector(conn));
                }
            }
        }
    }
}