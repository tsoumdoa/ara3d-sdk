using System.Linq;
using Ara3D.Utils;
using System.IO;
using System.IO.Compression;
using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Ara3D.Collections;
using Ara3D.Logging;
using Autodesk.Revit.DB;
using Parquet;
using Document = Autodesk.Revit.DB.Document;
using FilePath = Ara3D.Utils.FilePath;
using Material = Ara3D.Models.Material;

namespace Ara3D.Bowerbird.RevitSamples
{
    public static class BimOpenSchemaUtils
    {
        public static bool IsVis(Geometry g)
        {
            var demoPhase = g.Element.DemolishedPhaseId;
            if (demoPhase != ElementId.InvalidElementId)
                return false;
            var cat = g.Element.Category;
            if (cat == null)
                return false;
            if (cat.CategoryType != CategoryType.Model)
                return false;
            if (g.Element is SpatialElement)
                return false;
            return true;
        }

        public static BimGeometry ToBimGeometry(this Document doc, BimOpenSchemaRevitBuilder rbdb, bool recurseLinks)
        {
            var meshGatherer = new MeshGatherer(rbdb);
            var options = new Options()
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Fine,
            };

            meshGatherer.CollectMeshes(doc, options, recurseLinks, Transform.Identity);

            var builder = new BimGeometryBuilder();
            builder.Meshes.AddRange(meshGatherer.MeshList.Select(m => m.ToAra3D()));

            var geometries = meshGatherer.Geometries.Where(IsVis).ToList();
            foreach (var g in geometries)
            {
                if (g == null)
                    continue;

                var defaultMatIndex = builder.AddMaterial(g.DefaultMaterial ?? Material.Default);
                foreach (var part in g.Parts)
                {
                    var matIndex = part.Material == null 
                        ? defaultMatIndex 
                        : builder.AddMaterial(part.Material.Value);

                    var transformIndex = builder.AddTransform(part.Transform.ToAra3D());
                    var entityIndex = rbdb.GetEntityIndex(g.ElementKey);

                    builder.AddElement((int)entityIndex, matIndex, part.MeshIndex, transformIndex);
                }
            }

            return builder.BuildModel();
        }

        public static FilePath ExportBimOpenSchema(this Document currentDoc, BimOpenSchemaExportSettings settings, ILogger logger)
        {
            logger.Log($"Exporting BIM Open Schema Parquet Files");
            var bimDataBuilder = new BimOpenSchemaRevitBuilder(currentDoc, settings.IncludeLinks);
            var bimData = bimDataBuilder.Builder.Data;
            var dataSet = bimData.ToDataSet();

            var inputFile = new FilePath(currentDoc.PathName);
            var fp = inputFile.ChangeDirectoryAndExt(settings.Folder, settings.FileExtension);

            logger.Log($"Creating FileStream");
            var fs = new FileStream(fp, FileMode.Create, FileAccess.Write, FileShare.None);

            logger.Log($"Creating Zip Archive");
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

            var parquetCompressionMethod = CompressionMethod.Brotli;
            var parquetCompressionLevel = CompressionLevel.Optimal;
            var zipCompressionLevel = CompressionLevel.Fastest;

            logger.Log($"Creating FileStream");
            dataSet.WriteParquetToZip(zip,
            parquetCompressionMethod,
                    parquetCompressionLevel,
                    zipCompressionLevel);

            if (settings.IncludeGeometry)
            {
                logger.Log($"Creating BIM Geometry");
                var bimGeometry = ToBimGeometry(currentDoc, bimDataBuilder, settings.IncludeLinks);
                
                logger.Log($"Writing BIM geometry");
                bimGeometry.WriteParquetToZip(zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel);
            }

            logger.Log($"Finished writing to {fp}");
            return fp;
        }
    }
}
