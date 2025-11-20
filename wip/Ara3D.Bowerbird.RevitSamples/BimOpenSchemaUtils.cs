using System;
using System.Collections.Generic;
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
        public static BimGeometry ToBimGeometry(this Document doc, RevitBimDataBuilder rbdb, bool recurseLinks)
        {
            var meshGatherer = new MeshGatherer(rbdb);
            var options = new Options()
                {
                    // Because we are using a View, the view defines the detail level
                    //DetailLevel = ViewDetailLevel.Fine,
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = false,
                    View = doc.GetDefault3DView(),
                }; 

            meshGatherer.CollectMeshes(doc, options, recurseLinks, Transform.Identity);
            
            var builder = new BimGeometryBuilder();
            builder.Meshes.AddRange(meshGatherer.MeshList.Select(m => m.ToAra3D()));
            
            foreach (var g in meshGatherer.Geometries)
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
                    var entityIndex = rbdb.GetEntityIndex(g.SourceDocumentKey, g.ElementIdValue);

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
            var fp = inputFile.ChangeDirectoryAndExt(settings.Folder, ".parquet.zip");

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
