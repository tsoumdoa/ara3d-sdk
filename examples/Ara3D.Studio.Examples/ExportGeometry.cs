
namespace Ara3D.Studio.Samples
{
    
    public class ExportGeometry : IScriptedCommand
    {
        public string Name => "Export geometry";

        public static SaveFileDialog SaveFileDialog { get; } = new();

        public ExportGeometry() { }

        public void Execute(IHostApplication app)
        {
            /*
            SaveFileDialog.DefaultExt = "parquet";

            SaveFileDialog.Filter = "(*.parquet)|*.parquet|All Files (*.*)|*.*";
            
            if (SaveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var g = new SModel3D();
            foreach (var m in app.GetModels())
            {
                g.AddModel(m);
            }

            app.Logger.Log($"Total Size = {g.Size:N0}");
            app.Logger.Log($"  Model Size = {g.ModelCount * 4:N0}, Count = {g.ModelCount}");
            app.Logger.Log($"  Vertex Size = {g.VertexCount * 4:N0}, Count = {g.VertexCount / 3}");
            app.Logger.Log($"  Index Size = {g.IndexCount * 4:N0}, Count = {g.IndexCount / 3}");
            app.Logger.Log($"  Transform Size  = {g.TransformCount * 4:N0}, Count = {g.TransformCount / 16}");
            app.Logger.Log($"  Element Size = {g.ElementCount * 16:N0}, Count = {g.ElementCount}");
            app.Logger.Log($"  Mesh Size = {g.MeshCount * 8:N0}, Count = {g.MeshCount}");
            app.Logger.Log($"  Material Size = {g.MaterialCount * 8:N0}, Count = {g.MaterialCount}");
            */
        }

        public bool CanExecute(IHostApplication hostApplication)
            => hostApplication.GetModels().Any();
    }
}
