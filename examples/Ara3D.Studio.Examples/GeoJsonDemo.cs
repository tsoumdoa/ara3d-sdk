using Ara3D.IO.GeoJson;

namespace Ara3D.Studio.Samples;

public class GeoJsonDemo : IModelGenerator
{
    [Range(0f, 2f)] public float Thickness = 0.1f;
    [Range(0f, 10f)] public float Height = 2;
<<<<<<< HEAD
    public bool ShowFloors = false;

    public static IReadOnlyList<Point3D> ToPoints(IReadOnlyList<Vector3> vectors)
=======

    [Range(0f, 25f)] public float Offset = 5f;

    public static IReadOnlyList<Point3D> ToPoints(IReadOnlyList<System.Numerics.Vector3> vectors)
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
        => vectors.Select(v => new Point3D(v.X, v.Y, v.Z));

    public static IReadOnlyList<Line3D> LoopToLines(IReadOnlyList<Point3D> points)
        => points.Select((pt, i) => new Line3D(pt, points.ElementAtModulo(i + 1)));

<<<<<<< HEAD
=======

>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
    public Model3D Eval(EvalContext context)
    {
        var f = new FilePath(@"C:\Users\cdigg\AppData\Local\Temp\imdf.geojson");
        var fc = GeoJsonSerializer.LoadFeatureCollection(f.OpenRead());
        var mb = new Model3DBuilder();
        mb.Materials.Add(mb.DefaultMaterial);
        mb.Meshes.Add(PlatonicSolids.TriangulatedCube);

        var groups = fc.features.GroupBy(f => f["level_id"]);
<<<<<<< HEAD
        var roomIndex = 0;
=======
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
        foreach (var group in groups)
        {
            foreach (var feature in group)
            {
<<<<<<< HEAD
=======
                var offsetMatrix = Matrix4x4.CreateTranslation(new Vector3(Offset, Offset, Offset));

>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
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
<<<<<<< HEAD
                            var ti = mb.AddTransform(mat);
                            mb.ElementStructs.Add(new ElementStruct(roomIndex, 0, 0, ti));
                        }
                    }

                    if (loops.Length == 0 || loops[0].Length == 0) continue;
                    var height = loops[0][0].Z;

                    var loops2D = loops.Select(loop => loop.Select(v => v.To2D));
                    if (loops2D.Count > 0 && ShowFloors)
                    {
                        var outerLoop = loops2D[0];
                        var innerLoops = loops2D.Skip();
                        var floorPolygon = outerLoop.ToPolygonWithHoles(innerLoops.ToArray());
                        if (outerLoop.Count < 3) continue;
                        var newMesh = floorPolygon.TrianglesFan().ToMesh();
                        var newMeshIndex = mb.AddMesh(newMesh);
                        var mat = Matrix4x4.CreateTranslation(new(0, 0, height));
                        var ti = mb.AddTransform(mat);
                        mb.ElementStructs.Add(new ElementStruct(roomIndex, 0, newMeshIndex, ti));
                    }

                    roomIndex++;
=======
                            mat = mat * offsetMatrix;
                            var ti = mb.AddTransform(mat);
                            mb.ElementStructs.Add(new ElementStruct(0, 0, 0, ti));
                        }
                    }
>>>>>>> 22292231a48842c7a08bd4647b494e76a6ad633d
                }
            }
        }

        return mb.Build();
    }
}