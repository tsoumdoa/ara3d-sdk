using Ara3D.Geometry;
using Ara3D.Models;

namespace Ara3D.IfcLoader;

public static unsafe class IfcToModelConverters
{
    public static Model3D ToModel3D(this IfcFile file)
    {
        var mb = new Model3DBuilder();
        foreach (var g in file.Model.GetGeometries())
        {
            foreach (var mesh in g.GetMeshes())
            {
                var m = (double*)mesh.Transform;
                var c = (IfcColor*)mesh.Color;
                var color = new Color((float)c->R, (float)c->G, (float)c->B, (float)c->A);
                var mat = new Material(color, 0.1f, 0.5f);

                var matrix = new Matrix4x4(
                    (float)m[0], -(float)m[2], (float)m[1], (float)m[3],
                    (float)m[4], -(float)m[6], (float)m[5], (float)m[7],
                    (float)m[8], -(float)m[10], (float)m[9], (float)m[11],
                    (float)m[12], -(float)m[14], (float)m[13], (float)m[15]);

                mb.AddElement(mesh.ToTriangleMesh(), mat, matrix);
            }
        }
        return mb.Build();
    }

    // Copies the data from the C++ layer into arrays.
    public static unsafe TriangleMesh3D ToTriangleMesh(this IfcMesh m)
    {
        var vertexPtr = (IfcVertex*)m.Vertices;
        var vertices = new Point3D[m.NumVertices];
        for (var i = 0; i < m.NumVertices; i++)
        {
            var v = vertexPtr[i];
            vertices[i] = new Vector3(
                (float)v.PX, (float)v.PY, (float)v.PZ);
        }
        var indexPtr = (Integer*)m.Indices;
        var indices = new Integer3[m.NumIndices / 3];
        for (var i = 0; i < m.NumIndices / 3; i++)
        {
            indices[i] = new Integer3(
                indexPtr[i * 3], indexPtr[i * 3 + 1], indexPtr[i * 3 + 2]);
        }
        return new TriangleMesh3D(vertices, indices);
    }

}