using Ara3D.BimOpenSchema;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Document = Autodesk.Revit.DB.Document;
using Parameter = Ara3D.BimOpenSchema.Parameter;
using static Ara3D.BimOpenSchema.CommonRevitParameters;
using Domain = Autodesk.Revit.DB.Domain;
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
    public Dictionary<DocumentKey, Dictionary<int, EntityIndex>> ProcessedConnectors = new();
    public Dictionary<long, EntityIndex> ProcessedCategories = new();
    public int BoundaryCount;

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

    public PointIndex AddPoint(XYZ xyz)
        => AddPoint(Builder, xyz);

    public static PointIndex AddPoint(BimDataBuilder bdb, XYZ xyz)
        => bdb.AddPoint(new(xyz.X, xyz.Y, xyz.Z));

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

    public void AddParameter(EntityIndex ei, Parameter p, XYZ xyz)
        => AddParameter(ei, DescriptorLookup[p.Name], AddPoint(xyz));

    public void AddParameter(EntityIndex ei, Parameter p, string val)
        => AddParameter(ei, DescriptorLookup[p.Name], val ?? "");

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

        var r = Builder.AddEntity(
            category.Id.Value, 
            category.Id.ToString(), 
            CurrentDocumentIndex, 
            category.Name,
            category.BuiltInCategory.ToString());

        AddTypeAsParameter(r, category);
        AddParameter(r, CategoryCategoryType, category.CategoryType.ToString());
        AddParameter(r, CategoryBuiltInType, category.BuiltInCategory.ToString());

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

        var options = new SpatialElementBoundaryOptions();
        IList<IList<BoundarySegment>> segmentLists = room.GetBoundarySegments(options);

        var doc = room.Document;
        for (var i = 0; i < segmentLists.Count; i++)
        {
            var segmentList = segmentLists[i];
            foreach (var segment in segmentList)
            {
                ProcessBoundary(doc, ei, segment, i == 0);
            }
        }
    }

    public Element GetElement(Document d, ElementId id, ElementId linkedElementId)
    {
        var e = d.GetElement(id);
        if (e is RevitLinkInstance rli)
        {
            var d2 = rli.GetLinkDocument();
            if (d2 != null)
            {
                return d2.GetElement(linkedElementId);
            }
        }
        else
        {
            return e;
        }

        return null;
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

        var entityIndex = Builder.AddEntity(e.Id.Value, e.UniqueId, CurrentDocumentIndex, e.Name, catName);
        ProcessedEntities.Add(key, entityIndex);

        AddTypeAsParameter(entityIndex, e);

        if (category != null && category.IsValid)
        {
            var catIndex = ProcessCategory(category);
            AddParameter(entityIndex, ElementCategory, catIndex);
            Builder.AddRelation(entityIndex, catIndex, RelationType.MemberOf);
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

        if (e.Document.IsWorkshared)
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

        if (e is Level level)
            ProcessLevel(entityIndex, level);

        if (e is Family family)
            ProcessFamily(entityIndex, family);

        if (e is FamilyInstance familyInstance)
            ProcessFamilyInstance(entityIndex, familyInstance);

        if (e is Material material)
            ProcessMaterial(entityIndex, material);

        if (e is TextNote textNote)
            ProcessTextNote(entityIndex, textNote);

        if (e is SpatialElement se)
        {
            //if (e is Space space)
            //    throw new NotImplementedException();

            //if (e is Area area)
            //    throw new NotImplementedException();

            if (e is Room r)
                ProcessRoom(entityIndex, r);
        }

        if (e is MEPSystem sys)
        {
            if (sys.BaseEquipment is Element baseEquipment)
            {
                var baseEquipmentIndex = ProcessElement(baseEquipment);
                AddParameter(entityIndex, MepSystemBaseEquipment, baseEquipmentIndex);
            }

            AddParameter(entityIndex, MepSystemBaseEquipmentConnector, sys.BaseEquipmentConnector);

            AddParameter(entityIndex, MepSystemHasDesignParts, sys.HasDesignParts);
            AddParameter(entityIndex, MepSystemHasFabricationParts, sys.HasFabricationParts);
            AddParameter(entityIndex, MepSystemHasPlaceholders, sys.HasPlaceholders);
            AddParameter(entityIndex, MepSystemIsEmpty, sys.IsEmpty);
            AddParameter(entityIndex, MepSystemIsMultipleNetwork, sys.IsMultipleNetwork);
            AddParameter(entityIndex, MepSystemIsValid, sys.IsValid);
            AddParameter(entityIndex, MepSystemSectionsCount, sys.SectionsCount);

            foreach (Element terminal in sys.Elements)
            {
                var terminalEntityIndex = ProcessElement(terminal);
                Builder.AddRelation(terminalEntityIndex, entityIndex, RelationType.MemberOf);
            }
            
            if (e is MechanicalSystem ms)
            {
                AddParameter(entityIndex, MechSystemType, ms.SystemType.ToString());
                AddParameter(entityIndex, MechSystemIsWellConnected, ms.IsWellConnected);

                foreach (Element duct in ms.DuctNetwork)
                {
                    var ductEntityIndex = ProcessElement(duct);
                    Builder.AddRelation(ductEntityIndex, entityIndex, RelationType.MemberOf);
                }
            }

            if (e is ElectricalSystem es)
            {
                AddParameter(entityIndex, ElecSystemType, es.SystemType.ToString());
                AddParameter(entityIndex, ElecSystemApparentCurrent, es.ApparentCurrent);
                AddParameter(entityIndex, ElecSystemApparentLoad, es.ApparentLoad);
                AddParameter(entityIndex, ElecSystemBalancedLoad, es.BalancedLoad);
                AddParameter(entityIndex, ElecSystemCircuitConnectionType, es.CircuitConnectionType.ToString());
                AddParameter(entityIndex, ElecSystemCircuitType, es.CircuitType.ToString());
                AddParameter(entityIndex, ElecSystemCircuitNumber, es.CircuitNumber);
                AddParameter(entityIndex, ElecSystemFrame, es.Frame);
                AddParameter(entityIndex, ElecSystemHasCustomCircuitPath, es.HasCustomCircuitPath);
                AddParameter(entityIndex, ElecSystemHotConductorsNumber, es.HotConductorsNumber);
                AddParameter(entityIndex, ElecSystemIsBasePanelFeedThroughLugsOccupied, es.IsBasePanelFeedThroughLugsOccupied);
                AddParameter(entityIndex, ElecSystemLength, es.Length);
                AddParameter(entityIndex, ElecSystemLoadClassificationAbbreviations, es.LoadClassificationAbbreviations);
                AddParameter(entityIndex, ElecSystemLoadClassifications, es.LoadClassifications);
                AddParameter(entityIndex, ElecSystemLoadName, es.LoadName);
                AddParameter(entityIndex, ElecSystemNeutralConductorsNumber, es.NeutralConductorsNumber);
                AddParameter(entityIndex, ElecSystemPanelName, es.PanelName);
                AddParameter(entityIndex, ElecSystemPhaseLabel, es.PhaseLabel);
                AddParameter(entityIndex, ElecSystemPolesNumber, es.PolesNumber);
                AddParameter(entityIndex, ElecSystemPowerFactor, es.PowerFactor);
                AddParameter(entityIndex, ElecSystemPowerFactorState, es.PowerFactorState.ToString());
                AddParameter(entityIndex, ElecSystemRating, es.Rating);
                AddParameter(entityIndex, ElecSystemRunsNumber, es.RunsNumber);
                AddParameter(entityIndex, ElecSystemSlotIndex, es.SlotIndex);
                AddParameter(entityIndex, ElecSystemStartSlot, es.StartSlot);
                AddParameter(entityIndex, ElecSystemTrueCurrent, es.TrueCurrent);
                AddParameter(entityIndex, ElecSystemTrueLoad, es.TrueLoad);
                AddParameter(entityIndex, ElecSystemVoltage, es.Voltage);
                AddParameter(entityIndex, ElecSystemVoltageDrop, es.VoltageDrop);
                AddParameter(entityIndex, ElecSystemWays, es.Ways);
                AddParameter(entityIndex, ElecSystemWireSizeString, es.WireSizeString);
                AddParameter(entityIndex, ElecSystemWireType, es.WireType.ToString());
            }

            if (e is PipingSystem ps)
            {
                var pipesAndFittings = ps.PipingNetwork;

                foreach (Element pipeOrFitting in pipesAndFittings)
                {
                    var pipeElementIndex = ProcessElement(pipeOrFitting);
                    Builder.AddRelation(pipeElementIndex, entityIndex, RelationType.MemberOf);
                }

                AddParameter(entityIndex, PipingSystemTypeStr, ps.SystemType.ToString());
            }
        }

        if (e is Zone z)
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
            AddParameter(entityIndex, ZoneServiceType, z.ServiceType.ToString());

            var spaces = z.Spaces;
            foreach (Space space in spaces)
            {
                var spaceIndex = ProcessElement(space);
                Builder.AddRelation(entityIndex, spaceIndex, RelationType.ContainedIn);
            }
        }

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
        var ei = Builder.AddEntity(ProcessedDocuments.Count, CurrentDocument.CreationGUID.ToString(), CurrentDocumentIndex, d.Title, DocumentEntityName);
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

        ProcessConnectors();

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

    public EntityIndex ProcessConnector(Connector conn)
    {
        // LocalId = conn.Id (int), GlobalId = empty, Name = empty, Category = "Connector"
        var entityIndex = Builder.AddEntity(conn.Id, "", CurrentDocumentIndex, "", ConnectorEntityName);

        // Cache some things we’ll use a lot
        var domain = conn.Domain;
        var shape = conn.Shape;
        var owner = conn.Owner;
        bool ownerIsFamilyInstance = owner is FamilyInstance;

        // --- Basic identity / classification ---
        AddParameter(entityIndex, ConnectorId, conn.Id.ToString());
        AddParameter(entityIndex, ConnectorTypeStr, conn.ConnectorType.ToString());
        AddParameter(entityIndex, ConnectorShape, shape.ToString());
        AddParameter(entityIndex, ConnectorDomain, domain.ToString());
        AddParameter(entityIndex, ConnectorDescription, conn.Description);

        // --- Owner / system references ---
        if (owner is Element ownerElem)
        {
            var ownerEntityIndex = ProcessElement(ownerElem);
            AddParameter(entityIndex, ConnectorOwner, ownerEntityIndex);
            Builder.AddRelation(entityIndex, ownerEntityIndex, RelationType.MemberOf);
        }

        // --- Geometry / location ---
        if (conn.ConnectorType == ConnectorType.Physical)
        {
            var originIndex = AddPoint(Builder, conn.Origin);
            AddParameter(entityIndex, ConnectorOrigin, originIndex);
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
                AddParameter(entityIndex,
                    ConnectorAssignedDuctFlowConfiguration,
                    conn.AssignedDuctFlowConfiguration.ToString());

                AddParameter(entityIndex,
                    ConnectorAssignedDuctLossMethod,
                    conn.AssignedDuctLossMethod.ToString());

                AddParameter(entityIndex,
                    ConnectorAssignedLossCoefficient,
                    conn.AssignedLossCoefficient);
            }

            // Piping-only assigned properties
            if (domain == Domain.DomainPiping)
            {
                AddParameter(entityIndex, ConnectorAssignedFixtureUnits, conn.AssignedFixtureUnits);
                AddParameter(entityIndex, ConnectorAssignedKCoefficient, conn.AssignedKCoefficient);
                AddParameter(entityIndex, ConnectorAssignedPipeFlowConfiguration, conn.AssignedPipeFlowConfiguration.ToString());
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