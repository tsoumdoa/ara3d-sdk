using Ara3D.Utils;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Parquet;

namespace Ara3D.Bowerbird.RevitSamples
{
    public static class BimOpenSchemaUtils
    {
        public static void ExportBimOpenSchema(this Autodesk.Revit.DB.Document currentDoc, BimOpenSchemaExportSettings settings)
        {
            var bimDataBuilder = new RevitToOpenBimSchema(currentDoc, settings.IncludeLinks);
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
                var revitBuilder = new RevitBimGeometryBuilder();
                revitBuilder.ProcessDocument(currentDoc, settings.IncludeLinks);
                var bimGeometry = revitBuilder.Build();
                bimGeometry.WriteParquetToZip(zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel);
            }
        }


    }
}
