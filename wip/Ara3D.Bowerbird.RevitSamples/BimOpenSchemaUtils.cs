using System.Collections.Generic;
using Ara3D.Utils;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Ara3D.BimOpenGeometry;
using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Ara3D.Geometry;
using Autodesk.Revit.DB;
using Parquet;
using Document = Autodesk.Revit.DB.Document;
using FilePath = Ara3D.Utils.FilePath;
using Material = Ara3D.Models.Material;

namespace Ara3D.Bowerbird.RevitSamples
{
    public static class BimOpenSchemaUtils
    {
        public static BimGeometry ToBimGeometry(this Document doc, RevitBimDataBuilder rbdb, bool recurseLinks)
        {
            var meshGatherer = new MeshGatherer(rbdb);
            var options = MeshGatherer.DefaultGeometryOptions();
            meshGatherer.CollectMeshes(doc, options, recurseLinks, Transform.Identity);
            var builder = new BimGeometryBuilder();

            var meshLookup = new Dictionary<Mesh, int>();
            foreach (var mesh in meshGatherer.GetMeshes())
            {
                if (meshLookup.ContainsKey(mesh))
                    continue;

                meshLookup[mesh] = builder.AddMesh(mesh.ToAra3D());
            }
            
            foreach (var kv in meshGatherer.ElementGeometries)
            {
                var localDoc = kv.Key;
                foreach (var g in kv.Value)
                {
                    if (g == null)
                        continue;
                    var defaultMatIndex = builder.AddMaterial(ToMaterial(localDoc, g.DefaultMaterialId));
                    foreach (var part in g.Parts)
                    {
                        if (part?.Mesh == null)
                            continue;
                        var meshIndex = meshLookup[part.Mesh];
                        var matIndex = part.MaterialId == ElementId.InvalidElementId 
                            ? defaultMatIndex 
                            : builder.AddMaterial(ToMaterial(localDoc, part.MaterialId));

                        var transformIndex = builder.AddTransform(part.Transform.ToAra3D());

                        // TODO: this needs to be tracked in a separate builder. 
                        var entityIndex = rbdb.GetEntityIndex(doc, g.ElementId);

                        builder.AddElement(matIndex, meshIndex, transformIndex);
                    }
                }
            }

            return builder.BuildModel();
        }

        public static Material ToMaterial(PbrMaterialInfo pbr)
            => pbr == null
                ? Material.Default
                : new Material(pbr.BaseColor ?? pbr.ShadingColor, (float)(pbr.Metallic ?? 0),
                    (float)(pbr.Roughness ?? 0));

        public static Material ToMaterial(this Document doc, ElementId materialId)
        {
            if (materialId == ElementId.InvalidElementId)
                return Material.Default;
            var pbrMatInfo = doc.GetPbrInfo(materialId.Value);
            return ToMaterial(pbrMatInfo);
        }

        public static void ExportBimOpenSchema(this Document currentDoc, BimOpenSchemaExportSettings settings)
        {
            var bimDataBuilder = new RevitBimDataBuilder(currentDoc, settings.IncludeLinks);
            var bimData = bimDataBuilder.bdb.Data;
            var dataSet = bimData.ToDataSet();

            var inputFile = new FilePath(currentDoc.PathName);
            var fp = inputFile.ChangeDirectoryAndExt(settings.Folder, ".parquet.zip");

            var fs = new FileStream(fp, FileMode.Create, FileAccess.Write, FileShare.None);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

            var parquetCompressionMethod = CompressionMethod.Brotli;
            var parquetCompressionLevel = CompressionLevel.Optimal;
            var zipCompressionLevel = CompressionLevel.Fastest;

            Task.Run(() => dataSet.WriteParquetToZipAsync(zip,
                    parquetCompressionMethod,
                    parquetCompressionLevel,
                    zipCompressionLevel))
                .GetAwaiter().GetResult();

            if (settings.IncludeGeometry)
            {
                var bimGeometry = ToBimGeometry(currentDoc, bimDataBuilder, settings.IncludeLinks);
                bimGeometry.WriteParquetToZip(zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel);
            }
        }
    }
}
