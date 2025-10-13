using Ara3D.Utils;
namespace Ara3D.IO.GeoJson;

// https://docs.ogc.org/cs/20-094/Unit/index.html
// This is a room, zone, area, what have you. 
public class ImdfUnit : GeoJsonFeature
{
    public const string FeatureType = "unit";
    public string feature_type { get; set; } = FeatureType;
    public string id { get; set; }
    public GeoJsonPolygon geometry { get; set; }

    public ImdfUnitProperties GetProperties()
        => properties.SetProperties(new ImdfUnitProperties());

    public ImdfUnit SetProperties(ImdfUnitProperties props)
    {
        if (props == null)
            return this;
        properties ??= new();
        foreach (var kv in props.PropertiesToDictionary())
            properties.Add(kv.Key, kv.Value);
        return this;
    }

    public static ImdfUnit Create(string id, GeoJsonPolygon geometry, ImdfUnitProperties props = null)
        => new ImdfUnit()
        {
            id = id,
            geometry = geometry
        }.SetProperties(props);
}

public class ImdfUnitProperties
{
    public string level_id { get; set; }
    public string category { get; set; }
    public string restriction { get; set; }
    public string accessibility { get; set; }
    public string name { get; set; }
    public string alt_name { get; set; }
    public GeoJsonPoint display_point { get; set; } 

    // Additional non-standard properties
    public double? level_elevation { get; set; }
    public double? area { get; set; }
    public double? perimeter { get; set; }
}

// https://docs.ogc.org/cs/20-094/Opening/index.html
public class ImdfOpening : GeoJsonFeature
{
    public const string FeatureType = "opening";
    public string feature_type { get; set; } = FeatureType;
    public GeoJsonLineString geometry { get; set; }

    public ImdfOpening SetProperties(ImdfOpeningProperties props)
    {
        if (props == null)
            return this;
        properties ??= new();
        foreach (var kv in props.PropertiesToDictionary())
            properties.Add(kv.Key, kv.Value);
        return this;
    }

    public ImdfOpeningProperties GetProperties()
        => properties.SetProperties(new ImdfOpeningProperties ());

    public static ImdfOpening Create(string id, GeoJsonLineString geometry, ImdfOpeningProperties props = null)
        => new ImdfOpening()
        {
            id = id,
            geometry = geometry
        }.SetProperties(props);
}

public class ImdfOpeningProperties
{
    public string level_id { get; set; }
    public string category { get; set; }
    public string access_control { get; set; }
    public string accessibility { get; set; }
    public string name { get; set; }
    public string door { get; set; }
    public string alt_name { get; set; }
    public GeoJsonPoint display_point { get; set; }

    // Additional non-standard properties
    public string level_elevation { get; set; }
    public string from_room_id { get; set; }
    public string to_room_id { get; set; }
    public string family_name { get; set; }
}
