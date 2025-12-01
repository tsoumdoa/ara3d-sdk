using System.Collections.Generic;
using System.Reflection;

namespace Ara3D.BimOpenSchema;

public record Parameter(string Name, ParameterType Type)
{
    public static implicit operator string(Parameter p) => p.Name;
}

public static class CommonRevitParameters
{
    public const string DocumentEntityName = "__DOCUMENT__";
    public const string BoundaryEntityName = "__BOUNDARY__";
    public const string ConnectorEntityName = "__CONNECTOR__";

    // =========================
    // Object
    // =========================

    public static Parameter ObjectTypeName = new("Rvt:Object:TypeName", ParameterType.String);

    // =========================
    // Element
    // =========================
    
    public static Parameter ElementLevel = new("Rvt:Element:Level", ParameterType.Entity);
    public static Parameter ElementLocationPoint = new("Rvt:Element:Location.Point", ParameterType.Point);
    public static Parameter ElementLocationStartPoint = new("Rvt:Element:Location.StartPoint", ParameterType.Point);
    public static Parameter ElementLocationEndPoint = new("Rvt:Element:Location.EndPoint", ParameterType.Point);
    public static Parameter ElementBoundsMin = new("Rvt:Element:Bounds.Min", ParameterType.Point);
    public static Parameter ElementBoundsMax = new("Rvt:Element:Bounds.Max", ParameterType.Point);
    public static Parameter ElementAssemblyInstance = new("Rvt:Element:AssemblyInstance", ParameterType.Entity);
    public static Parameter ElementDesignOption = new("Rvt:Element:DesignOption", ParameterType.Entity);
    public static Parameter ElementGroup = new("Rvt:Element:Group", ParameterType.Entity);
    public static Parameter ElementWorksetId = new("Rvt:Element:WorksetId", ParameterType.Int);
    public static Parameter ElementCreatedPhase = new("Rvt:Element:CreatedPhase", ParameterType.Entity);
    public static Parameter ElementDemolishedPhase = new("Rvt:Element:DemolishedPhase", ParameterType.Entity);
    public static Parameter ElementCategory = new("Rvt:Element:Category", ParameterType.Entity);
    public static Parameter ElementIsViewSpecific = new("Rvt:Element:IsViewSpecific", ParameterType.Int);
    public static Parameter ElementOwnerView = new("Rvt:Element:OwnerView", ParameterType.Entity);

    // =========================
    // FamilyInstance
    // =========================

    public static Parameter FIToRoom = new("Rvt:FamilyInstance:ToRoom", ParameterType.Entity);
    public static Parameter FIFromRoom = new("Rvt:FamilyInstance:FromRoom", ParameterType.Entity);
    public static Parameter FIHost = new("Rvt:FamilyInstance:Host", ParameterType.Entity);
    public static Parameter FISpace = new("Rvt:FamilyInstance:Space", ParameterType.Entity);
    public static Parameter FIRoom = new("Rvt:FamilyInstance:Room", ParameterType.Entity);
    public static Parameter FIFamilyType = new("Rvt:FamilyInstance:FamilyType", ParameterType.Entity);
    public static Parameter FIStructuralUsage = new("Rvt:FamilyInstance:StructuralUsage", ParameterType.String);
    public static Parameter FIStructuralMaterialType = new("Rvt:FamilyInstance:StructuralMaterialType", ParameterType.String);
    public static Parameter FIStructuralMaterial = new("Rvt:FamilyInstance:StructuralMaterial", ParameterType.Entity);
    public static Parameter FIStructuralType = new("Rvt:FamilyInstance:StructuralType", ParameterType.String);

    // =========================
    // Family
    // =========================

    public static Parameter FamilyStructuralCodeName = new("Rvt:Family:StructuralCodeName", ParameterType.String);
    public static Parameter FamilyStructuralMaterialType = new("Rvt:Family:StructuralMaterialType", ParameterType.String);

    // =========================
    // Room
    // =========================

    public static Parameter RoomNumber = new("Rvt:Room:Number", ParameterType.String);
    public static Parameter RoomBaseOffset = new("Rvt:Room:BaseOffset", ParameterType.Double);
    public static Parameter RoomLimitOffset = new("Rvt:Room:LimitOffset", ParameterType.Double);
    public static Parameter RoomUnboundedHeight = new("Rvt:Room:UnboundedHeight", ParameterType.Double);
    public static Parameter RoomVolume = new("Rvt:Room:Volume", ParameterType.Double);
    public static Parameter RoomUpperLimit = new("Rvt:Room:UpperLimit", ParameterType.Entity);

    // =========================
    // Level
    // =========================

    public static Parameter LevelProjectElevation = new("Rvt:Level:ProjectElevation", ParameterType.Double);
    public static Parameter LevelElevation = new("Rvt:Level:Elevation", ParameterType.Double);

    // =========================
    // Material
    // =========================

    public static Parameter MaterialColorRed = new("Rvt:Material:Color.Red", ParameterType.Double);
    public static Parameter MaterialColorGreen = new("Rvt:Material:Color.Green", ParameterType.Double);
    public static Parameter MaterialColorBlue = new("Rvt:Material:Color.Blue", ParameterType.Double);
    public static Parameter MaterialShininess = new("Rvt:Material:Shininess", ParameterType.Double);
    public static Parameter MaterialSmoothness = new("Rvt:Material:Smoothness", ParameterType.Double);
    public static Parameter MaterialCategory = new("Rvt:Material:Category", ParameterType.String);
    public static Parameter MaterialClass = new("Rvt:Material:Class", ParameterType.String);
    public static Parameter MaterialTransparency = new("Rvt:Material:Transparency", ParameterType.Double);

    // =========================
    // TextNote
    // =========================

    public static Parameter TextNoteCoord = new("Rvt:TextNote:Coord", ParameterType.Point);
    public static Parameter TextNoteDir = new("Rvt:TextNote:Dir", ParameterType.Point);
    public static Parameter TextNoteText = new("Rvt:TextNote:Text", ParameterType.String);
    public static Parameter TextNoteWidth = new("Rvt:TextNote:Width", ParameterType.Double);
    public static Parameter TextNoteHeight = new("Rvt:TextNote:Height", ParameterType.Double);

    // =========================
    // Workset
    // =========================
    
    public static Parameter WorksetKind = new("Rvt:Workset:Kind", ParameterType.String);

    // =========================
    // Layer
    // =========================

    public static Parameter LayerIndex = new("Rvt:Layer:Index", ParameterType.Int);
    public static Parameter LayerFunction = new("Rvt:Layer:Function", ParameterType.String);
    public static Parameter LayerWidth = new("Rvt:Layer:Width", ParameterType.Double);
    public static Parameter LayerMaterialId = new("Rvt:Layer:MaterialId", ParameterType.Entity);
    public static Parameter LayerIsCore = new("Rvt:Layer:IsCore", ParameterType.Int);

    // =========================
    // Document
    // =========================

    public static Parameter DocumentCreationGuid = new("Rvt:Document:CreationGuid", ParameterType.String);
    public static Parameter DocumentWorksharingGuid = new("Rvt:Document:WorksharingGuid", ParameterType.String);
    public static Parameter DocumentTitle = new("Rvt:Document:Title", ParameterType.String);
    public static Parameter DocumentPath = new("Rvt:Document:Path", ParameterType.String);
    public static Parameter DocumentElevation = new("Rvt:Document:Elevation", ParameterType.Double);
    public static Parameter DocumentLatitude = new("Rvt:Document:Latitude", ParameterType.Double);
    public static Parameter DocumentLongitude = new("Rvt:Document:Longitude", ParameterType.Double);
    public static Parameter DocumentPlaceName = new("Rvt:Document:PlaceName", ParameterType.String);
    public static Parameter DocumentWeatherStationName = new("Rvt:Document:WeatherStationName", ParameterType.String);
    public static Parameter DocumentTimeZone = new("Rvt:Document:TimeZone", ParameterType.Double);
    public static Parameter DocumentLastSaveTime = new("Rvt:Document:LastSaveTime", ParameterType.String);
    public static Parameter DocumentSaveCount = new("Rvt:Document:SaveCount", ParameterType.Int);
    public static Parameter DocumentIsDetached = new("Rvt:Document:IsDetached", ParameterType.Int);
    public static Parameter DocumentIsLinked = new("Rvt:Document:IsLinked", ParameterType.Int);

    // =========================
    // Project
    // =========================
    
    public static Parameter ProjectName = new("Rvt:Document:Project:Name", ParameterType.String);
    public static Parameter ProjectNumber = new("Rvt:Document:Project:Number", ParameterType.String);
    public static Parameter ProjectStatus = new("Rvt:Document:Project:Status", ParameterType.String);
    public static Parameter ProjectAddress = new("Rvt:Document:Project:Address", ParameterType.String);
    public static Parameter ProjectClientName = new("Rvt:Document:Project:Client", ParameterType.String);
    public static Parameter ProjectIssueDate = new("Rvt:Document:Project:IssueDate", ParameterType.String);
    public static Parameter ProjectAuthor = new("Rvt:Document:Project:Author", ParameterType.String);
    public static Parameter ProjectBuildingName = new("Rvt:Document:Project:BuildingName", ParameterType.String);
    public static Parameter ProjectOrgDescription = new("Rvt:Document:Project:OrganizationDescription", ParameterType.String);
    public static Parameter ProjectOrgName = new("Rvt:Document:Project:OrganizationName", ParameterType.String);

    // =========================
    // Boundary 
    // =========================
    
    public static Parameter BoundaryOuter = new("Rvt:Boundary:Outer", ParameterType.Int);
    public static Parameter BoundaryElement = new("Rvt:Boundary:Element", ParameterType.Entity);

    // =========================
    // Category
    // =========================
    
    public static Parameter CategoryCategoryType = new("Rvt:Category:CategoryType", ParameterType.String);
    public static Parameter CategoryBuiltInType = new("Rvt:Category:BuiltInType", ParameterType.String);

    // =========================
    // Zone
    // =========================

    // NOTE: we do not include Calculated properties
    public static Parameter ZoneArea = new("Rvt:Zone:Area", ParameterType.Double);
    public static Parameter ZoneCoolingAirTemperature = new("Rvt:Zone:CoolingAirTemperature", ParameterType.Double);
    public static Parameter ZoneCoolingSetPoint = new("Rvt:Zone:CoolingSetPoint", ParameterType.Double);
    public static Parameter ZoneDehumidificationSetPoint = new("Rvt:Zone:DehumidificationSetPoint", ParameterType.Double);
    public static Parameter ZoneGrossArea = new("Rvt:Zone:GrossArea", ParameterType.Double);
    public static Parameter ZoneGrossVolume = new("Rvt:Zone:GrossVolume", ParameterType.Double);
    public static Parameter ZoneHeatingAirTemperature = new("Rvt:Zone:HeatingAirTemperature", ParameterType.Double);
    public static Parameter ZoneHeatingSetPoint = new("Rvt:Zone:HeatingSetPoint", ParameterType.Double);
    public static Parameter ZonePerimeter = new("Rvt:Zone:Perimeter", ParameterType.Double);
    public static Parameter ZoneServiceType = new("Rvt:Zone:ServiceType", ParameterType.String);

    // =========================
    // MEP System (base)
    // =========================

    public static Parameter MepSystemHasDesignParts = new("Rvt:MepSystem:HasDesignParts", ParameterType.Int);
    public static Parameter MepSystemHasFabricationParts = new("Rvt:MepSystem:HasFabricationParts", ParameterType.Int);
    public static Parameter MepSystemHasPlaceholders = new("Rvt:MepSystem:HasPlaceholders", ParameterType.Int);
    public static Parameter MepSystemIsEmpty = new("Rvt:MepSystem:IsEmpty", ParameterType.Int);
    public static Parameter MepSystemIsMultipleNetwork = new("Rvt:MepSystem:IsMultipleNetwork", ParameterType.Int);
    public static Parameter MepSystemIsValid = new("Rvt:MepSystem:IsValid", ParameterType.Int);
    public static Parameter MepSystemSectionsCount = new("Rvt:MepSystem:SectionsCount", ParameterType.Int);
    public static Parameter MepSystemBaseEquipment = new("Rvt:MepSystem:BaseEquipment", ParameterType.Entity);
    public static Parameter MepSystemBaseEquipmentConnector = new("Rvt:MepSystem:BaseEquipmentConnector", ParameterType.Entity);

    // =========================
    // Mechanical System
    // =========================

    public static Parameter MechSystemType = new("Rvt:MechanicalSystem:SystemType", ParameterType.String);
    public static Parameter MechSystemIsWellConnected = new("Rvt:MechanicalSystem:IsWellConnected", ParameterType.Int);

    // =========================
    // Electrical System
    // =========================

    public static Parameter ElecSystemType = new("Rvt:ElectricalSystem:SystemType", ParameterType.String);
    public static Parameter ElecSystemApparentCurrent = new("Rvt:ElectricalSystem:ApparentCurrent", ParameterType.Double);
    public static Parameter ElecSystemApparentLoad = new("Rvt:ElectricalSystem:ApparentLoad", ParameterType.Double);
    public static Parameter ElecSystemBalancedLoad = new("Rvt:ElectricalSystem:BalancedLoad", ParameterType.Int);
    public static Parameter ElecSystemCircuitConnectionType = new("Rvt:ElectricalSystem:CircuitConnectionType", ParameterType.String);
    public static Parameter ElecSystemCircuitType = new("Rvt:ElectricalSystem:CircuitType", ParameterType.String);
    public static Parameter ElecSystemCircuitNumber = new("Rvt:ElectricalSystem:CircuitNumber", ParameterType.String);
    public static Parameter ElecSystemFrame = new("Rvt:ElectricalSystem:Frame", ParameterType.Double);
    public static Parameter ElecSystemHasCustomCircuitPath = new("Rvt:ElectricalSystem:HasCustomCircuitPath", ParameterType.Int);
    public static Parameter ElecSystemHotConductorsNumber = new("Rvt:ElectricalSystem:HotConductorsNumber", ParameterType.Int);
    public static Parameter ElecSystemIsBasePanelFeedThroughLugsOccupied = new("Rvt:ElectricalSystem:IsBasePanelFeedThroughLugsOccupied", ParameterType.Int);
    public static Parameter ElecSystemLength = new("Rvt:ElectricalSystem:Length", ParameterType.Double);
    public static Parameter ElecSystemLoadClassificationAbbreviations = new("Rvt:ElectricalSystem:LoadClassificationAbbreviations", ParameterType.String);
    public static Parameter ElecSystemLoadClassifications = new("Rvt:ElectricalSystem:LoadClassifications", ParameterType.String);
    public static Parameter ElecSystemLoadName = new("Rvt:ElectricalSystem:LoadName", ParameterType.String);
    public static Parameter ElecSystemNeutralConductorsNumber = new("Rvt:ElectricalSystem:NeutralConductorsNumber", ParameterType.Int);
    public static Parameter ElecSystemPanelName = new("Rvt:ElectricalSystem:PanelName", ParameterType.String);
    public static Parameter ElecSystemPhaseLabel = new("Rvt:ElectricalSystem:PhaseLabel", ParameterType.String);
    public static Parameter ElecSystemPolesNumber = new("Rvt:ElectricalSystem:PolesNumber", ParameterType.Int);
    public static Parameter ElecSystemPowerFactor = new("Rvt:ElectricalSystem:PowerFactor", ParameterType.Double);
    public static Parameter ElecSystemPowerFactorState = new("Rvt:ElectricalSystem:PowerFactorState", ParameterType.String);
    public static Parameter ElecSystemRating = new("Rvt:ElectricalSystem:Rating", ParameterType.Double);
    public static Parameter ElecSystemRunsNumber = new("Rvt:ElectricalSystem:RunsNumber", ParameterType.Int);
    public static Parameter ElecSystemSlotIndex = new("Rvt:ElectricalSystem:SlotIndex", ParameterType.String);
    public static Parameter ElecSystemStartSlot = new("Rvt:ElectricalSystem:StartSlot", ParameterType.Int);
    public static Parameter ElecSystemTrueCurrent = new("Rvt:ElectricalSystem:TrueCurrent", ParameterType.Double);
    public static Parameter ElecSystemTrueLoad = new("Rvt:ElectricalSystem:TrueLoad", ParameterType.Double);
    public static Parameter ElecSystemVoltage = new("Rvt:ElectricalSystem:Voltage", ParameterType.Double);
    public static Parameter ElecSystemVoltageDrop = new("Rvt:ElectricalSystem:VoltageDrop", ParameterType.Double);
    public static Parameter ElecSystemWays = new("Rvt:ElectricalSystem:Ways", ParameterType.Int);
    public static Parameter ElecSystemWireSizeString = new("Rvt:ElectricalSystem:WireSizeString", ParameterType.String);
    public static Parameter ElecSystemWireType = new("Rvt:ElectricalSystem:WireType", ParameterType.String);

    // =========================
    // Connector
    // =========================

    public static Parameter ConnectorAllowsSlopeAdjustments = new("Rvt:Connector:AllowsSlopeAdjustments", ParameterType.Int);
    public static Parameter ConnectorAngle = new("Rvt:Connector:Angle", ParameterType.Double);
    public static Parameter ConnectorAssignedDuctFlowConfiguration = new("Rvt:Connector:AssignedDuctFlowConfiguration", ParameterType.String);
    public static Parameter ConnectorAssignedDuctLossMethod = new("Rvt:Connector:AssignedDuctLossMethod", ParameterType.String);
    public static Parameter ConnectorAssignedFixtureUnits = new("Rvt:Connector:AssignedFixtureUnits", ParameterType.Double);
    public static Parameter ConnectorAssignedFlow = new("Rvt:Connector:AssignedFlow", ParameterType.Double);
    public static Parameter ConnectorAssignedFlowDirection = new("Rvt:Connector:AssignedFlowDirection", ParameterType.String);
    public static Parameter ConnectorAssignedFlowFactor = new("Rvt:Connector:AssignedFlowFactor", ParameterType.Double);
    public static Parameter ConnectorAssignedKCoefficient = new("Rvt:Connector:AssignedKCoefficient", ParameterType.Double);
    public static Parameter ConnectorAssignedLossCoefficient = new("Rvt:Connector:AssignedLossCoefficient", ParameterType.Double);
    public static Parameter ConnectorAssignedPipeFlowConfiguration = new("Rvt:Connector:AssignedPipeFlowConfiguration", ParameterType.String);
    public static Parameter ConnectorAssignedPipeLossMethod = new("Rvt:Connector:AssignedPipeLossMethod", ParameterType.String);
    public static Parameter ConnectorAssignedPressureDrop = new("Rvt:Connector:AssignedPressureDrop", ParameterType.Double);
    public static Parameter ConnectorCoefficient = new("Rvt:Connector:Coefficient", ParameterType.Double);
    public static Parameter ConnectorDemand = new("Rvt:Connector:Demand", ParameterType.Double);
    public static Parameter ConnectorFlow = new("Rvt:Connector:Flow", ParameterType.Double);
    public static Parameter ConnectorPressureDrop = new("Rvt:Connector:PressureDrop", ParameterType.Double);
    public static Parameter ConnectorVelocityPressure = new("Rvt:Connector:VelocityPressure", ParameterType.Double);
    public static Parameter ConnectorHeight = new("Rvt:Connector:Height", ParameterType.Double);
    public static Parameter ConnectorWidth = new("Rvt:Connector:Width", ParameterType.Double);
    public static Parameter ConnectorRadius = new("Rvt:Connector:Radius", ParameterType.Double);
    public static Parameter ConnectorEngagementLength = new("Rvt:Connector:EngagementLength", ParameterType.Double);
    public static Parameter ConnectorId = new("Rvt:Connector:Id", ParameterType.String); // int, but safest as string
    public static Parameter ConnectorTypeStr = new("Rvt:Connector:Type", ParameterType.String);
    public static Parameter ConnectorShape = new("Rvt:Connector:Shape", ParameterType.String);
    public static Parameter ConnectorDomain = new("Rvt:Connector:Domain", ParameterType.String);
    public static Parameter ConnectorDuctSystemType = new("Rvt:Connector:DuctSystemType", ParameterType.String);
    public static Parameter ConnectorElectricalSystemType = new("Rvt:Connector:ElectricalSystemType", ParameterType.String);
    public static Parameter ConnectorPipeSystemType = new("Rvt:Connector:PipeSystemType", ParameterType.String);
    public static Parameter ConnectorUtility = new("Rvt:Connector:Utility", ParameterType.Int);
    public static Parameter ConnectorDescription = new("Rvt:Connector:Description", ParameterType.String);
    public static Parameter ConnectorOrigin = new("Rvt:Connector:Origin", ParameterType.Point);
    public static Parameter ConnectorCoordinateSystem = new("Rvt:Connector:CoordinateSystem", ParameterType.String);
    public static Parameter ConnectorOwner = new("Rvt:Connector:Owner", ParameterType.Entity);
    public static Parameter ConnectorDirection = new("Rvt:Connector:Direction", ParameterType.String);
    public static Parameter ConnectorIsConnected = new("Rvt:Connector:IsConnected", ParameterType.Int);
    public static Parameter ConnectorIsMovable = new("Rvt:Connector:IsMovable", ParameterType.Int);
    public static Parameter ConnectorGasketLength = new("Rvt:Connector:GasketLength", ParameterType.Double);

    // =========================
    // Piping System
    // =========================

    public static Parameter PipingSystemTypeStr = new("Rvt:PipingSystem:PipingSystemType", ParameterType.String);

    /// <summary>
    /// Returns a UI friendly version of the parameter name
    /// </summary>
    public static string ParameterNameToUI(string name)
        => name.Substring(name.LastIndexOf(':') + 1);

    public static IEnumerable<Parameter> GetParameters()
    {
        foreach (var fi in typeof(CommonRevitParameters).GetFields(
                     BindingFlags.Static | BindingFlags.Public))
        {
            var p = fi.GetValue(null) as Parameter;
            if (p != null)
                yield return p;
        }
    }
}