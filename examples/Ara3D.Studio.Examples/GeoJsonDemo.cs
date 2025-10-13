using Ara3D.IO.GeoJson;

namespace Ara3D.Studio.Samples;

public class GeoJsonDemo : IModelGenerator
{
    [Range(0f, 2f)] public float Thickness = 0.1f;
    [Range(0f, 10f)] public float Height = 2;

    [Range(0f, 25f)] public float Offset = 5f;

    public static IReadOnlyList<Point3D> ToPoints(IReadOnlyList<System.Numerics.Vector3> vectors)
        => vectors.Select(v => new Point3D(v.X, v.Y, v.Z));

    public static IReadOnlyList<Line3D> LoopToLines(IReadOnlyList<Point3D> points)
        => points.Select((pt, i) => new Line3D(pt, points.ElementAtModulo(i + 1)));


    public Model3D Eval(EvalContext context)
    {
        var f = new FilePath(@"C:\Users\cdigg\AppData\Local\Temp\imdf.geojson");
        var fc = GeoJsonSerializer.LoadFeatureCollection(f.OpenRead());
        var mb = new Model3DBuilder();
        mb.Materials.Add(mb.DefaultMaterial);
        mb.Meshes.Add(PlatonicSolids.TriangulatedCube);

        var groups = fc.features.GroupBy(f => f["level_id"]);
        foreach (var group in groups)
        {
            foreach (var feature in group)
            {
                var offsetMatrix = Matrix4x4.CreateTranslation(new Vector3(Offset, Offset, Offset));

                if (feature is ImdfUnit imdfUnit)
                {
                    var geo = imdfUnit.geometry;
                    var loops = geo.coordinates.ToVector3Arrays();
                    foreach (var loop in loops)
                    {
                        var lines = LoopToLines(ToPoints(loop));
                        foreach (var line in lines)
                        {
                            var mat = line.ToBoxTransform(Thickness, Height);
                            mat = mat * offsetMatrix;
                            var ti = mb.AddTransform(mat);
                            mb.ElementStructs.Add(new ElementStruct(0, 0, 0, ti));
                        }
                    }
                }
            }
        }

        return mb.Build();
    }
}