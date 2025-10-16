<<<<<<< HEAD
﻿namespace Ara3D.IO.GeoJson;
=======
﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ara3D.IO.GeoJson;
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d

public abstract class GeoJsonObject
{
    public abstract string type { get; set; }
}

public abstract class GeoJsonGeometry : GeoJsonObject
{
}

public class GeoJsonFeature : GeoJsonObject
{
    public const string TypeValue = "Feature";
    public override string type { get; set; } = TypeValue;
    public GeoJsonGeometry? geometry { get; set; }
    public Dictionary<string, object>? properties { get; set; }
    public string? id { get; set; }
    public object this[string name] => properties.TryGetValue(name, out var val) ? val : null;
}

public class GeoJsonPoint : GeoJsonGeometry
{
    public const string TypeValue = "Point";
    public override string type { get; set; } = TypeValue;
    public double[] coordinates { get; set; } = [];
}

public class GeoJsonLineString : GeoJsonGeometry
{
    public const string TypeValue = "LineString";
    public override string type { get; set; } = TypeValue;
    public double[][] coordinates { get; set; } = [];
}

public class GeoJsonPolygon : GeoJsonGeometry
{
    public const string TypeValue = "Polygon"; 
    public override string type { get; set; } = TypeValue;
    public double[][][] coordinates { get; set; } = [];
}

public class GeoJsonFeatureCollection : GeoJsonObject
{
    public const string TypeValue = "FeatureCollection";
    public Dictionary<string, object> properties { get; set; }
    public override string type { get; set; } = TypeValue;
    public List<GeoJsonFeature> features { get; set; } = [];
}