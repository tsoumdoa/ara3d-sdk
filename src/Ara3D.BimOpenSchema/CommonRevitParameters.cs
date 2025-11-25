using System.Collections.Generic;
using System.Reflection;

namespace Ara3D.BimOpenSchema;

public record Parameter(string Name, ParameterType Type)
{
    public static implicit operator string(Parameter p) => p.Name;
}

public static class CommonRevitParameters
{

    // Object
    public static Parameter ObjectTypeName =
        new("rvt:Object:TypeName", ParameterType.String);

    // Element
    public static Parameter ElementLevel =
        new("rvt:Element:Level", ParameterType.Entity);

    public static Parameter ElementLocationPoint =
        new("rvt:Element:Location.Point", ParameterType.Point);

    public static Parameter ElementLocationStartPoint =
        new("rvt:Element:Location.StartPoint", ParameterType.Point);

    public static Parameter ElementLocationEndPoint =
        new("rvt:Element:Location.EndPoint", ParameterType.Point);

    public static Parameter ElementBoundsMin =
        new("rvt:Element:Bounds.Min", ParameterType.Point);

    public static Parameter ElementBoundsMax =
        new("rvt:Element:Bounds.Max", ParameterType.Point);

    public static Parameter ElementAssemblyInstance =
        new("rvt:Element:AssemblyInstance", ParameterType.Entity);

    public static Parameter ElementDesignOption =
        new("rvt:Element:DesignOption", ParameterType.Entity);

    public static Parameter ElementGroup =
        new("rvt:Element:Group", ParameterType.Entity);

    public static Parameter ElementWorksetId =
        new("rvt:Element:WorksetId", ParameterType.Int);

    public static Parameter ElementCreatedPhase =
        new("rvt:Element:CreatedPhase", ParameterType.Entity);

    public static Parameter ElementDemolishedPhase =
        new("rvt:Element:DemolishedPhase", ParameterType.Entity);

    public static Parameter ElementCategory =
        new("rvt:Element:Category", ParameterType.Entity);

    public static Parameter ElementIsViewSpecific =
        new("rvt:Element:IsViewSpecific", ParameterType.Int);

    public static Parameter ElementOwnerView =
        new("rvt:Element:OwnerView", ParameterType.Entity);

    // FamilyInstance
    public static Parameter FIToRoom =
        new("rvt:FamilyInstance:ToRoom", ParameterType.Entity);

    public static Parameter FIFromRoom =
        new("rvt:FamilyInstance:FromRoom", ParameterType.Entity);

    public static Parameter FIHost =
        new("rvt:FamilyInstance:Host", ParameterType.Entity);

    public static Parameter FISpace =
        new("rvt:FamilyInstance:Space", ParameterType.Entity);

    public static Parameter FIRoom =
        new("rvt:FamilyInstance:Room", ParameterType.Entity);

    public static Parameter FIFamilyType =
        new("rvt:FamilyInstance:FamilyType", ParameterType.Entity);

    public static Parameter FIStructuralUsage =
        new("rvt:FamilyInstance:StructuralUsage", ParameterType.String);

    public static Parameter FIStructuralMaterialType =
        new("rvt:FamilyInstance:StructuralMaterialType", ParameterType.String);

    public static Parameter FIStructuralMaterial =
        new("rvt:FamilyInstance:StructuralMaterial", ParameterType.Entity);

    public static Parameter FIStructuralType =
        new("rvt:FamilyInstance:StructuralType", ParameterType.String);

    // Family
    public static Parameter FamilyStructuralCodeName =
        new("rvt:Family:StructuralCodeName", ParameterType.String);

    public static Parameter FamilyStructuralMaterialType =
        new("rvt:Family:StructuralMaterialType", ParameterType.String);

    // Room
    public static Parameter RoomNumber =
        new("rvt:Room:Number", ParameterType.String);

    public static Parameter RoomBaseOffset =
        new("rvt:Room:BaseOffset", ParameterType.Double);

    public static Parameter RoomLimitOffset =
        new("rvt:Room:LimitOffset", ParameterType.Double);

    public static Parameter RoomUnboundedHeight =
        new("rvt:Room:UnboundedHeight", ParameterType.Double);

    public static Parameter RoomVolume =
        new("rvt:Room:Volume", ParameterType.Double);

    public static Parameter RoomUpperLimit =
        new("rvt:Room:UpperLimit", ParameterType.Entity);

    // Level
    public static Parameter LevelProjectElevation =
        new("rvt:Level:ProjectElevation", ParameterType.Double);

    public static Parameter LevelElevation =
        new("rvt:Level:Elevation", ParameterType.Double);

    // Material
    public static Parameter MaterialColorRed =
        new("rvt:Material:Color.Red", ParameterType.Double);

    public static Parameter MaterialColorGreen =
        new("rvt:Material:Color.Green", ParameterType.Double);

    public static Parameter MaterialColorBlue =
        new("rvt:Material:Color.Blue", ParameterType.Double);

    public static Parameter MaterialShininess =
        new("rvt:Material:Shininess", ParameterType.Double);

    public static Parameter MaterialSmoothness =
        new("rvt:Material:Smoothness", ParameterType.Double);

    public static Parameter MaterialCategory =
        new("rvt:Material:Category", ParameterType.String);

    public static Parameter MaterialClass =
        new("rvt:Material:Class", ParameterType.String);

    public static Parameter MaterialTransparency =
        new("rvt:Material:Transparency", ParameterType.Double);

    // Workset
    public static Parameter WorksetKind =
        new("rvt:Workset:Kind", ParameterType.String);

    // Layer
    public static Parameter LayerIndex =
        new("rvt:Layer:Index", ParameterType.Int);

    public static Parameter LayerFunction =
        new("rvt:Layer:Function", ParameterType.String);

    public static Parameter LayerWidth =
        new("rvt:Layer:Width", ParameterType.Double);

    public static Parameter LayerMaterialId =
        new("rvt:Layer:MaterialId", ParameterType.Entity);

    public static Parameter LayerIsCore =
        new("rvt:Layer:IsCore", ParameterType.Int);

    // Document
    public static Parameter DocumentCreationGuid =
        new("rvt:Document:CreationGuid", ParameterType.String);

    public static Parameter DocumentWorksharingGuid =
        new("rvt:Document:WorksharingGuid", ParameterType.String);

    public static Parameter DocumentTitle =
        new("rvt:Document:Title", ParameterType.String);

    public static Parameter DocumentPath =
        new("rvt:Document:Path", ParameterType.String);

    public static Parameter DocumentElevation =
        new("rvt:Document:Elevation", ParameterType.Double);

    public static Parameter DocumentLatitude =
        new("rvt:Document:Latitude", ParameterType.Double);

    public static Parameter DocumentLongitude =
        new("rvt:Document:Longitude", ParameterType.Double);

    public static Parameter DocumentPlaceName =
        new("rvt:Document:PlaceName", ParameterType.String);

    public static Parameter DocumentWeatherStationName =
        new("rvt:Document:WeatherStationName", ParameterType.String);

    public static Parameter DocumentTimeZone =
        new("rvt:Document:TimeZone", ParameterType.String);

    public static Parameter DocumentLastSaveTime =
        new("rvt:Document:LastSaveTime", ParameterType.String);

    public static Parameter DocumentSaveCount =
        new("rvt:Document:SaveCount", ParameterType.Int);

    public static Parameter DocumentIsDetached =
        new("rvt:Document:IsDetached", ParameterType.Int);

    public static Parameter DocumentIsLinked =
        new("rvt:Document:IsLinked", ParameterType.Int);

    // Project
    public static Parameter ProjectName =
        new("rvt:Document:Project:Name", ParameterType.String);

    public static Parameter ProjectNumber =
        new("rvt:Document:Project:Number", ParameterType.String);

    public static Parameter ProjectStatus =
        new("rvt:Document:Project:Status", ParameterType.String);

    public static Parameter ProjectAddress =
        new("rvt:Document:Project:Address", ParameterType.String);

    public static Parameter ProjectClientName =
        new("rvt:Document:Project:Client", ParameterType.String);

    public static Parameter ProjectIssueDate =
        new("rvt:Document:Project:IssueDate", ParameterType.String);

    public static Parameter ProjectAuthor =
        new("rvt:Document:Project:Author", ParameterType.String);

    public static Parameter ProjectBuildingName =
        new("rvt:Document:Project:BuildingName", ParameterType.String);

    public static Parameter ProjectOrgDescription =
        new("rvt:Document:Project:OrganizationDescription", ParameterType.String);

    public static Parameter ProjectOrgName =
        new("rvt:Document:Project:OrganizationName", ParameterType.String);

    // Category
    public static Parameter CategoryCategoryType =
        new("rvt:Category:CategoryType", ParameterType.String);

    public static Parameter CategoryBuiltInType =
        new("rvt:Category:BuiltInType", ParameterType.String);

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
            yield return p;
        }
    }
}