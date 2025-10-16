<<<<<<< HEAD
﻿using Ara3D.Geometry;
=======
﻿using System.Numerics;
using System.Runtime.CompilerServices;
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
using Ara3D.Collections;

namespace Ara3D.IO.GeoJson;

public static class ImdfHelpers
{
    public static double[] ToGeoJsonCoordinates(this Vector2 v)
        => [v.X, v.Y];

    public static double[] ToGeoJsonCoordinates(this Vector3 v)
        => [v.X, v.Y, v.Z];

<<<<<<< HEAD
    public static double[][] ToGeoJsonCoordinates(this IReadOnlyList<Vector2> v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][] ToGeoJsonCoordinates(this IReadOnlyList<Vector3> v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][][] ToGeoJsonCoordinates(this IReadOnlyList<IReadOnlyList<Vector2>> v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][][] ToGeoJsonCoordinates(this IReadOnlyList<IReadOnlyList<Vector3>> v)
=======
    public static double[][] ToGeoJsonCoordinates(this Vector2[] v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][] ToGeoJsonCoordinates(this Vector3[] v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][][] ToGeoJsonCoordinates(this Vector2[][] v)
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static double[][][] ToGeoJsonCoordinates(this Vector3[][] v)
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
        => v.Select(ToGeoJsonCoordinates).ToArray();

    public static GeoJsonPoint ToGeoJson(this Vector2 v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonPoint ToGeoJson(this Vector3 v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

<<<<<<< HEAD
    public static GeoJsonLineString ToGeoJson(this IReadOnlyList<Vector2> v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonLineString ToGeoJson(this IReadOnlyList<Vector3> v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonPolygon ToGeoJson(this IReadOnlyList<IReadOnlyList<Vector2>> v)
        => new() { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonPolygon ToGeoJson(this IReadOnlyList<IReadOnlyList<Vector3>> v)
=======
    public static GeoJsonLineString ToGeoJson(this Vector2[] v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonLineString ToGeoJson(this Vector3[] v)
        => new () { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonPolygon ToGeoJson(this Vector2[][] v)
        => new() { coordinates = v.ToGeoJsonCoordinates() };

    public static GeoJsonPolygon ToGeoJson(this Vector3[][] v)
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
        => new() { coordinates = v.ToGeoJsonCoordinates() };

    public static Vector2 ToVector2(this IReadOnlyList<double> self)
        => new(self.Count > 0 ? (float)self[0] : 0, self.Count > 1 ? (float)self[1] : 0);

    public static Vector3 ToVector3(this IReadOnlyList<double> self)
        => new(self.Count > 0 ? (float)self[0] : 0, self.Count > 1 ? (float)self[1] : 0, self.Count > 2 ? (float)self[2] : 0);

    public static Vector2[] ToVector2Array(this double[][] values)
        => values.Select(xs => xs.ToVector2()).ToArray();

    public static Vector3[] ToVector3Array(this double[][] values)
        => values.Select(xs => xs.ToVector3()).ToArray();

    public static int MinDimension<T>(this T[][] self)
        => self.Min(xs => xs.Length);

    public static int MaxDimension<T>(this T[][] self)
        => self.Max(xs => xs.Length);

    public static int MinDimension<T>(this T[][][] self)
        => self.Min(xs => xs.MinDimension());

    public static int MaxDimension<T>(this T[][][] self)
        => self.Max(xs => xs.MaxDimension());

    public static Vector2[][] ToVector2Arrays(this double[][][] self)
        => self.Select(xs => xs.ToVector2Array()).ToArray();

    public static Vector3[][] ToVector3Arrays(this double[][][] self)
        => self.Select(xs => xs.ToVector3Array()).ToArray();

    public static bool Is3D(this GeoJsonPolygon self)
        => self.coordinates.MaxDimension() >= 3;

    public static bool Is3D(this GeoJsonLineString self)
        => self.coordinates.MaxDimension() >= 3;

    public static bool Is3D(this GeoJsonPoint self)
        => self.coordinates.Length >= 3;

<<<<<<< HEAD
    public static IReadOnlyList<IReadOnlyList<Vector2>> GetVector2Loops(this GeoJsonPolygon self)
        => self.coordinates.ToVector2Arrays();

    public static IReadOnlyList<IReadOnlyList<Vector3>> GetVector3Loops(this GeoJsonPolygon self)
        => self.coordinates.ToVector3Arrays();

    public static IReadOnlyList<Vector2> GetVector2Points(this GeoJsonLineString self)
        => self.coordinates.ToVector2Array();

    public static IReadOnlyList<Vector3> GetVector3Points(this GeoJsonLineString self)
=======
    public static Vector2[][] GetVector2Loops(this GeoJsonPolygon self)
        => self.coordinates.ToVector2Arrays();

    public static Vector3[][] GetVector3Loops(this GeoJsonPolygon self)
        => self.coordinates.ToVector3Arrays();

    public static Vector2[] GetVector2Points(this GeoJsonLineString self)
        => self.coordinates.ToVector2Array();

    public static Vector3[] GetVector3Points(this GeoJsonLineString self)
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
        => self.coordinates.ToVector3Array();
}