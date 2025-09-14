namespace Ara3D.Studio.Samples;

public class SceneStats : IScriptedCommand
{
    public string Name => "Scene Stats";

    public void Execute(IHostApplication app)
    {
        var g = new ColumnarGeometry();
        foreach (var m in app.GetModels())
            g.AddModel(m);

        app.Logger.Log($"Total Size = {g.Size:N0}");
        app.Logger.Log($"  Model Size = {g.ModelCount * 4:N0}, Count = {g.ModelCount:N0}");
        app.Logger.Log($"  Vertex Size = {g.VertexCount * 4:N0}, Count = {g.VertexCount / 3:N0}");
        app.Logger.Log($"  Index Size = {g.IndexCount * 4:N0}, Count = {g.IndexCount / 3:N0}");
        app.Logger.Log($"  Transform Size  = {g.TransformCount * 4:N0}, Count = {g.TransformCount / 16:N0}");
        app.Logger.Log($"  Element Size = {g.ElementCount * 16:N0}, Count = {g.ElementCount:N0}");
        app.Logger.Log($"  Mesh Size = {g.MeshCount * 8:N0}, Count = {g.MeshCount:N0}");
        app.Logger.Log($"  Material Size = {g.MaterialCount * 8:N0}, Count = {g.MaterialCount:N0}");
    }

    public bool CanExecute(IHostApplication hostApplication)
        => true;
}