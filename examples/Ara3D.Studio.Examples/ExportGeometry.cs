
namespace Ara3D.Studio.Samples
{
    public class ColumnarGeometry
    {
        public List<uint> ModelElementOffsets = new();
        public List<float> Vertices = new();
        public List<uint> Indices = new();
        public List<float> Transforms = new();
        public List<uint> ElementMeshIndex = new();
        public List<uint> ElementObjectId = new();
        public List<uint> ElementMaterialIndex = new();
        public List<uint> ElementTransformIndex = new();
        public List<uint> MeshVertexOffsets = new();
        public List<uint> MeshIndexOffsets = new();
        public List<ulong> Material = new();

        public long Size => ModelCount * 4L 
                            + VertexCount * 4L 
                            + IndexCount * 4L 
                            + TransformCount * 4L 
                            + ElementCount * 16L 
                            + MeshCount * 8L 
                            + MaterialCount * 8L;

        public int ModelCount => ModelElementOffsets.Count;
        public int ElementCount => ElementMeshIndex.Count;
        public int MaterialCount => Material.Count;
        public int MeshCount => MeshIndexOffsets.Count;
        public int VertexCount => Vertices.Count;
        public int IndexCount => Indices.Count;
        public int TransformCount => Transforms.Count;

        public ulong EncodeMaterial(Material m)
        {
            var r = m.Color.R.Value.ToByteFromNormalized();
            var g = m.Color.G.Value.ToByteFromNormalized();
            var b = m.Color.B.Value.ToByteFromNormalized();
            var a = m.Color.A.Value.ToByteFromNormalized();
            var k = m.Metallic.ToByteFromNormalized();
            var f = m.Roughness.ToByteFromNormalized();
            return (ulong)r & 
                   (ulong)g << 8 &
                   (ulong)b << 16 &
                   (ulong)a << 24 &
                   (ulong)k << 32 &
                   (ulong)f << 40;
        }

        public void AddModel(Model3D model)
        {
            var elementOffset = (uint)ElementCount;
            var meshOffset = (uint)MeshCount;
            var materialOffset = (uint)MaterialCount;
            var transformOffset = (uint)TransformCount;

            ModelElementOffsets.Add(elementOffset);

            foreach (var transform in model.Transforms)
            {
                foreach (var c in transform.Components)
                    Transforms.Add(c);
            }

            foreach (var material in model.Materials)
            {
                var matCode = EncodeMaterial(material);
                Material.Add(matCode);
            }

            foreach (var mesh in model.Meshes)
            {
                MeshVertexOffsets.Add((uint)VertexCount);
                MeshIndexOffsets.Add((uint)IndexCount);

                foreach (var p in mesh.Points)
                {
                    Vertices.AddRange([p.X, p.Y, p.Z]);
                }

                foreach (var f in mesh.FaceIndices)
                {
                    Indices.AddRange([
                        (uint)f.A.Value, 
                        (uint)f.B.Value, 
                        (uint)f.C.Value]);
                }
            }

            foreach (var es in model.ElementStructs)
            {
                ElementTransformIndex.Add((uint)es.TransformIndex + transformOffset);
                ElementMaterialIndex.Add((uint)es.MaterialIndex + materialOffset);
                ElementMeshIndex.Add((uint)es.MeshIndex + meshOffset);
                ElementObjectId.Add((uint)es.ElementIndex + elementOffset);
            }
        }
    }

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
            */

            var g = new ColumnarGeometry();
            foreach (var m in app.GetModels())
            {
                g.AddModel(m);
            }

            /*
            public long Size => ModelCount * 4L
                                + VertexCount * 4L
                                + IndexCount * 4L
                                + TransformCount * 4L
                                + ElementCount * 16L
                                + MeshCount * 8L
                                + MaterialCount * 8L;
            */
            app.Logger.Log($"Total Size = {g.Size:N0}");
            app.Logger.Log($"  Model Size = {g.ModelCount * 4:N0}, Count = {g.ModelCount}");
            app.Logger.Log($"  Vertex Size = {g.VertexCount * 4:N0}, Count = {g.VertexCount / 3}");
            app.Logger.Log($"  Index Size = {g.IndexCount * 4:N0}, Count = {g.IndexCount / 3}");
            app.Logger.Log($"  Transform Size  = {g.TransformCount * 4:N0}, Count = {g.TransformCount / 16}");
            app.Logger.Log($"  Element Size = {g.ElementCount * 16:N0}, Count = {g.ElementCount}");
            app.Logger.Log($"  Mesh Size = {g.MeshCount * 8:N0}, Count = {g.MeshCount}");
            app.Logger.Log($"  Material Size = {g.MaterialCount * 8:N0}, Count = {g.MaterialCount}");
        }

        public bool CanExecute(IHostApplication hostApplication)
            => hostApplication.GetModels().Any();
    }
}
