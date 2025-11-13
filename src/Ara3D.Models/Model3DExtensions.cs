using Ara3D.Collections;
using Ara3D.DataTable;
using Ara3D.Geometry;
using Ara3D.Memory;
using System.Data;
using System.Diagnostics.Contracts;

namespace Ara3D.Models;

public static class Model3DExtensions
{
    public static TriangleMesh3D EmptyMesh = new([], []);

    public static Integer3 Offset(Integer3 self, Integer offset)
        => (self.A + offset, self.B + offset, self.C + offset);

    public static TriangleMesh3D GetMesh(this Model3D self, InstanceStruct node)
        => node.MeshIndex < 0 ? EmptyMesh : self.Meshes[node.MeshIndex];

    public static TriangleMesh3D ToMesh(this Model3D self)
    {
        var points = new List<Point3D>();
        var indices = new List<Integer3>();
        var indexOffset = 0;

        foreach (var node in self.Instances)
        {
            var mesh = self.GetMesh(node);
            var mat = node.Matrix4x4;

            if (!mat.Equals(Matrix4x4.Identity))
            {
                foreach (var p in mesh.Points)
                    points.Add(p.Transform(mat));
            }
            else
            {
                // Fast path
                points.AddRange(mesh.Points);
            }

            if (indexOffset != 0)
            {
                foreach (var f in mesh.FaceIndices)
                    indices.Add(Offset(f, indexOffset));
            }
            else
            {
                // Fast path
                indices.AddRange(mesh.FaceIndices);
            }

            indexOffset = points.Count;
        }

        // TODO: we need  to be able to work more efficiently with buffers 
        return new TriangleMesh3D(points, indices);
    }

    public static IReadOnlyList<Point3D> TransformedPoints(this Model3D self)
    {
        var points = new UnmanagedList<Point3D>();

        foreach (var node in self.Instances)
        {
            var mesh = self.GetMesh(node);
            var mat = node.Matrix4x4;

            if (!mat.Equals(Matrix4x4.Identity))
            {
                foreach (var p in mesh.Points)
                    points.Add(mat.Transform(p));
            }
            else
            {
                points.AddRange(mesh.Points);
            }
        }

        return points;
    }

    public static Bounds3D GetBounds(this Model3D self)
    {
        var r = new Bounds3D();
        var meshBounds = self.Meshes.Map(m => m.Bounds).ToArray();
        foreach (var node in self.Instances)
        {
            if (node.MeshIndex < 0) continue;
            var rawBounds = meshBounds[node.MeshIndex];
            var mat = node.Matrix4x4;
            var lclBounds = rawBounds.Transform(mat);
            r = r.Include(lclBounds);
        }
        return r;
    }

    public static Model3D WithMeshes(this Model3D self, Func<TriangleMesh3D, TriangleMesh3D> f)
        => self.WithMeshes(self.Meshes.Select(f));

    public static Model3D WithInstances(this Model3D self, Func<InstanceStruct, InstanceStruct> f)
        => self.WithInstances(self.Instances.Select(f));

    public static Model3D Where(this Model3D self, Func<InstanceStruct, bool> f)
        => self.WithInstances(self.Instances.Where(f).ToList());

    public static Model3D Where(this Model3D self, Func<TriangleMesh3D, bool> f)
        => self.WithInstances(self.Instances.Where(i => f(self.GetMesh(i))).ToList());

    public static Model3D Where(this Model3D self, Func<InstanceStruct, int, bool> f)
        => self.WithInstances(self.Instances.Where(f).ToList());

    public static Model3D Clone(this Model3D model, IReadOnlyList<Vector3> positions)
        => model.Clone(positions.Map(Matrix4x4.CreateTranslation));

    public static Model3D Clone(this Model3D model, IReadOnlyList<Matrix4x4> matrices)
        => model.WithInstances(
            model.Instances.SelectMany(
                node => matrices.Select(m => node.Transform(m))).ToList());
    
    public static Model3D Clone(this TriangleMesh3D mesh, Material material, IReadOnlyList<Point3D> points)
        => mesh.Clone(material, points.Map(p => Matrix4x4.CreateTranslation(p.Vector3)));

    public static Model3D Clone(this TriangleMesh3D mesh, Material material, IReadOnlyList<Matrix4x4> transforms)
        => Model3D.Create(mesh, material, transforms);

    public static Model3D CloneAlong(this TriangleMesh3D mesh, Func<Number, Point3D> curveFunc, Integer count)
    {
        var transforms = count.LinearSpaceExclusive.Map(curveFunc).Map(p => Matrix4x4.CreateTranslation(p));
        return Clone(mesh, Material.Default, transforms);
    }

    public static Material FirstOrDefaultMaterial(this Model3D self)
        => self.Instances.Count > 0 ? self.Instances[0].Material : Material.Default;

    public static Model3D AddColumnsToTable(this Model3D self, string tableName, IReadOnlyList<IDataColumn> columns)
        => self.WithDataSet(self.DataSet.AddColumnsToTable(tableName, columns));

    public static Model3D MergeTable(this Model3D self, IDataTable table)
        => self.WithDataSet(self.DataSet.AddColumnsToTable(table.Name, table.Columns));

    public static System.Data.DataTable GetSystemDataTable(this Model3D self, string name)
        => self?.DataSet?.GetTable(name)?.ToSystemDataTable();
}