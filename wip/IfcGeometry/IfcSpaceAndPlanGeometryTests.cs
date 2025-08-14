using Ara3D.IfcGeometry;
using Ara3D.Utils;

public static class IfcSpaceAndPlanGeometryTests
{
    public static string[] SpatialElements = 
@"IfcProject
IfcSite
IfcBuilding
IfcBuildingStorey
IfcSpace
IfcExternalSpatialElement
IfcZone
IfcSpatialZone"
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

	public static string[] Elements = 
@"IfcWall
IfcWallStandardCase
IfcDoor
IfcDoorType
IfcWindow
IfcOpeningElement
IfcStair
IfcRamp
IfcSlab
IfcRailing
IfcRelAggregates
IfcRelContainedInSpatialStructure
IfcRelAssignsToGroup
IfcRelDefinesByProperties
IfcElementQuantity
IfcRelSpaceBoundary
IfcRelSpaceBoundary1stLevel
IfcRelSpaceBoundary2ndLevel
IfcRelVoidsElement
IfcRelFillsElement
IfcProductDefinitionShape
IfcShapeRepresentation
IfcGeometricRepresentationSubContext
IfcLocalPlacement
IfcBoundingBox
IfcMappedItem".Split(['\r','\n'], StringSplitOptions.RemoveEmptyEntries);


	[Test]
    public static void FindRepsInXSD()
	{
		var elements = IfcXsdParserTests.GetIfcElements().ToDictionary(e => e.Attribute("name")?.Value, e => e);
		foreach (var elemName in Elements)
		{
			if (elements.TryGetValue(elemName, out var typeName))
			{
				Console.WriteLine($"{elemName} : {typeName}");
			}
			else
			{
				Console.WriteLine($"{elemName} : NOT FOUND");
			}
		}
	}

    public static void GetRooms()
    {
    }
}