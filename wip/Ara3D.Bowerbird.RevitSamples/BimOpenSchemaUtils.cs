using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Ara3D.Utils;
using System.IO;
using System.IO.Compression;
using System.Text;
using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Ara3D.Collections;
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

        public static BimGeometry ToBimGeometry(this Document doc, RevitBimDataBuilder rbdb, bool recurseLinks)
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

        public static void ExportBimOpenSchema(this Document currentDoc, BimOpenSchemaExportSettings settings, StringBuilder sb = null)
        {
            var sw = Stopwatch.StartNew();

            sb?.AppendLine($"Exporting BOS");
            var bimDataBuilder = new RevitBimDataBuilder(currentDoc, settings.IncludeLinks);
            var bimData = bimDataBuilder.Builder.Data;
            var dataSet = bimData.ToDataSet();

            var inputFile = new FilePath(currentDoc.PathName);
            var fp = inputFile.ChangeDirectoryAndExt(settings.Folder, settings.FileExtension);

            sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Creating FileStream");
            var fs = new FileStream(fp, FileMode.Create, FileAccess.Write, FileShare.None);

            sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Zip Archive");
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

            var parquetCompressionMethod = CompressionMethod.Brotli;
            var parquetCompressionLevel = CompressionLevel.Optimal;
            var zipCompressionLevel = CompressionLevel.Fastest;

            sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Writing Parquet");
            dataSet.WriteParquetToZip(zip,
            parquetCompressionMethod,
                    parquetCompressionLevel,
                    zipCompressionLevel);

            if (settings.IncludeGeometry)
            {
                sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Creating BIM Geometry");
                var bimGeometry = ToBimGeometry(currentDoc, bimDataBuilder, settings.IncludeLinks);
                
                sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Writing BIM geometry");
                bimGeometry.WriteParquetToZip(zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel);
            }

            sb?.AppendLine($"{sw.PrettyPrintTimeElapsed()} - Finished");
        }
    }
}
