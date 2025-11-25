namespace Ara3D.BimOpenSchema;

public static class CommonRevitParameters
{
    // Object
    public const string ObjectTypeName = "rvt:Object:TypeName";

    // Element
    public const string ElementLevel = "rvt:Element:Level";
    public const string ElementLocationPoint = "rvt:Element:Location.Point";
    public const string ElementLocationStartPoint = "rvt:Element:Location.StartPoint";
    public const string ElementLocationEndPoint = "rvt:Element:Location.EndPoint";
    public const string ElementBoundsMin = "rvt:Element:Bounds.Min";
    public const string ElementBoundsMax = "rvt:Element:Bounds.Max";
    public const string ElementAssemblyInstance = "rvt:Element:AssemblyInstance";
    public const string ElementDesignOption = "rvt:Element:DesignOption";
    public const string ElementGroup = "rvt:Element:Group";
    public const string ElementWorkset = "rvt:Element:Workset";
    public const string ElementCreatedPhase = "rvt:Element:CreatedPhase";
    public const string ElementDemolishedPhase = "rvt:Element:DemolishedPhase";
    public const string ElementCategory = "rvt:Element:Category";
    public const string ElementIsViewSpecific = "rvt:Element:IsViewSpecific";
    public const string ElementOwnerView = "rvt:Element:OwnerView";

    // FamilyInstance
    public const string FIToRoom = "rvt:FamilyInstance:ToRoom";
    public const string FIFromRoom = "rvt:FamilyInstance:FromRoom";
    public const string FIHost = "rvt:FamilyInstance:Host";
    public const string FISpace = "rvt:FamilyInstance:Space";
    public const string FIRoom = "rvt:FamilyInstance:Room";
    public const string FIFamilyType = "rvt:FamilyInstance:FamilyType";
    public const string FIStructuralUsage = "rvt:FamilyInstance:StructuralUsage";
    public const string FIStructuralMaterialType = "rvt:FamilyInstance:StructuralMaterialType";
    public const string FIStructuralMaterialId = "rvt:FamilyInstance:StructuralMaterialId";
    public const string FIStructuralType = "rvt:FamilyInstance:StructuralType";

    // Family
    public const string FamilyStructuralCodeName = "rvt:Family:StructuralCodeName";
    public const string FamilyStructuralMaterialType = "rvt:Family:StructuralMaterialType";

    // Room
    public const string RoomNumber = "rvt:Room:Number";
    public const string RoomBaseOffset = "rvt:Room:BaseOffset";
    public const string RoomLimitOffset = "rvt:Room:LimitOffset";
    public const string RoomUnboundedHeight = "rvt:Room:UnboundedHeight";
    public const string RoomVolume = "rvt:Room:Volume";
    public const string RoomUpperLimit = "rvt:Room:UpperLimit";

    // Level
    public const string LevelProjectElevation = "rvt:Level:ProjectElevation";
    public const string LevelElevation = "rvt:Level:Elevation";

    // Material
    public const string MaterialColorRed = "rvt:Material:Color.Red";
    public const string MaterialColorGreen = "rvt:Material:Color.Green";
    public const string MaterialColorBlue = "rvt:Material:Color.Blue";
    public const string MaterialShininess = "rvt:Material:Shininess";
    public const string MaterialSmoothness = "rvt:Material:Smoothness";
    public const string MaterialCategory = "rvt:Material:Category";
    public const string MaterialClass = "rvt:Material:Class";
    public const string MaterialTransparency = "rvt:Material:Transparency";

    // Workset
    public const string WorksetKind = "rvt:Workset:Kind";

    // Layer
    public const string LayerIndex = "rvt:Layer:Index";
    public const string LayerFunction = "rvt:Layer:Function";
    public const string LayerWidth = "rvt:Layer:Width";
    public const string LayerMaterialId = "rvt:Layer:MaterialId";
    public const string LayerIsCore = "rvt:Layer:IsCore";

    // Document
    public const string DocumentCreationGuid = "rvt:Document:CreationGuid";
    public const string DocumentWorksharingGuid = "rvt:Document:WorksharingGuid";
    public const string DocumentTitle = "rvt:Document:Title";
    public const string DocumentPath = "rvt:Document:Path";
    public const string DocumentElevation = "rvt:Document:Elevation";
    public const string DocumentLatitude = "rvt:Document:Latitude";
    public const string DocumentLongitude = "rvt:Document:Longitude";
    public const string DocumentPlaceName = "rvt:Document:PlaceName";
    public const string DocumentWeatherStationName = "rvt:Document:WeatherStationName";
    public const string DocumentTimeZone = "rvt:Document:TimeZone";
    public const string DocumentLastSaveTime = "rvt:Document:LastSaveTime";
    public const string DocumentSaveCount = "rvt:Document:SaveCount";
    public const string DocumentIsDetached = "rvt:Document:IsDetached";
    public const string DocumentIsLinked = "rvt:Document:IsLinked";
    
    // Project
    public const string ProjectName = "rvt:Document:Project:Name";
    public const string ProjectNumber = "rvt:Document:Project:Number";
    public const string ProjectStatus = "rvt:Document:Project:Status";
    public const string ProjectAddress = "rvt:Document:Project:Address";
    public const string ProjectClientName = "rvt:Document:Project:Client";
    public const string ProjectIssueDate = "rvt:Document:Project:IssueDate";
    public const string ProjectAuthor = "rvt:Document:Project:Author";
    public const string ProjectBuildingName = "rvt:Document:Project:BuildingName";
    public const string ProjectOrgDescription = "rvt:Document:Project:OrganizationDescription";
    public const string ProjectOrgName = "rvt:Document:Project:OrganizationName";

    // Category
    public const string CategoryCategoryType = "rvt:Category:CategoryType";
    public const string CategoryBuiltInType = "rvt:Category:BuiltInType";

    /// <summary>
    /// Returns a UI friendly version of the parameter name
    /// </summary>
    public static string ParameterNameToUI(string name)
        => name.Substring(name.LastIndexOf(':') + 1);
}