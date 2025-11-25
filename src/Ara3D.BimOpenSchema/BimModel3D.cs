using Ara3D.DataTable;
using Ara3D.Models;

namespace Ara3D.BimOpenSchema;

public class BimModel3D : IModel3D
{
    public BimModel3D(IModel3D model, IDataSet set)
    {
        Model = model;
        DataSet = set;
    }

    public IModel3D Model { get; }
    public IDataSet DataSet { get; }

    public void UpdateScene(RenderScene scene)
        => Model.UpdateScene(scene);

    public void Dispose()
    { }
}