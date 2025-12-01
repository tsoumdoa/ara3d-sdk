using System.Collections.Generic;
using Ara3D.Geometry;
using Ara3D.Models;

namespace Ara3D.BimOpenSchema;

public class BimModel3D : IModel3D
{
    public BimModel3D(BimObjectModel model)
    {
        ObjectModel = model;
        Model3D = ObjectModel.Data.Geometry.ToModel3D();
    }

    public Model3D Model3D { get; private set; }
    public BimObjectModel ObjectModel { get; }

    public IReadOnlyList<TriangleMesh3D> Meshes => Model3D.Meshes;
    public IReadOnlyList<InstanceStruct> Instances => Model3D.Instances;

    public void UpdateScene(RenderScene scene)
        => Model3D.UpdateScene(scene);

    public void Dispose()
    {
        Model3D.Dispose();
        Model3D = null;
    }

    public static BimModel3D Create(BimObjectModel model)
        => new(model);

    public static BimModel3D Create(BimData data)
        => new(new BimObjectModel(data));
}