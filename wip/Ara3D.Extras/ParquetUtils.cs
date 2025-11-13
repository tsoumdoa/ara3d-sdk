using Ara3D.DataTable;
using Ara3D.Utils;
using Parquet;
using Parquet.Schema;
using System.IO.Compression;
using Ara3D.BimOpenGeometry;
using DataColumn = Parquet.Data.DataColumn;

namespace Ara3D.BimOpenSchema.IO;

public static class ParquetUtils
{
    public static async Task WriteParquetAsync(
        this IDataTable table,
        FilePath filePath,
        CompressionLevel level = CompressionLevel.Optimal,
        CompressionMethod method = CompressionMethod.Brotli)
    {
        await using var fs = File.Create(filePath);
        await table.WriteParquetAsync(fs, level, method);
    }

    public static async Task WriteParquetAsync(
        this IDataTable table,
        Stream stream,
        CompressionLevel level = CompressionLevel.Optimal,
        CompressionMethod method = CompressionMethod.Brotli)
    {
        var dataFields = table.Columns.Select(c => new DataField(c.Descriptor.Name, c.Descriptor.Type)).ToList();
        var schema = new ParquetSchema(dataFields);

        await using var writer = await ParquetWriter.CreateAsync(schema, stream);
        writer.CompressionLevel = level;
        writer.CompressionMethod = method;
        using var rg = writer.CreateRowGroup();

        foreach (var c in table.Columns)
        {
            var df = dataFields[c.ColumnIndex];
            var array = Array.CreateInstance(c.Descriptor.Type, c.Count);
            for (var i = 0; i < c.Count; i++)
                array.SetValue(c[i], i);
            var dc = new DataColumn(df, array);
            await rg.WriteColumnAsync(dc);
        }
    }

    public static void WriteParquetToZip(
        this IDataSet set,
        FilePath zipPath,
        CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli,
        CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
        CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
        => Task.Run(() => set.WriteParquetToZipAsync(zipPath, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel))
            .GetAwaiter().GetResult();

    public static async Task WriteParquetToZipAsync(this IDataSet set, FilePath zipPath,
            CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli,
            CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
            CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
    {
        await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);
        await WriteParquetToZipAsync(set, zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel);
    }

    public static void WriteParquetToZip(
        this IDataSet set,
        ZipArchive zip,
        CompressionMethod parquetCompressionMethod,
        CompressionLevel parquetCompressionLevel,
        CompressionLevel zipCompressionLevel)
        => Task.Run(() =>
                set.WriteParquetToZipAsync(zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel))
            .GetAwaiter().GetResult();

    public static async Task WriteParquetToZipAsync(
        this IDataSet set,
        ZipArchive zip,
        CompressionMethod parquetCompressionMethod,
        CompressionLevel parquetCompressionLevel,
        CompressionLevel zipCompressionLevel)
    {
        foreach (var table in set.Tables)
        {
            var entryName = $"{table.Name}.parquet";
            var entry = zip.CreateEntry(entryName, zipCompressionLevel);
            await using var parquetBuffer = new MemoryStream();
            await table.WriteParquetAsync(parquetBuffer, parquetCompressionLevel, parquetCompressionMethod);
            parquetBuffer.Position = 0;
            await using var entryStream = entry.Open();
            await parquetBuffer.CopyToAsync(entryStream);
        }
    }

    public static async Task<IDataTable> ReadParquetAsync(this FilePath filePath, string? name = null)
    {
        name ??= filePath.GetFileNameWithoutExtension();
        var reader = await ParquetReader.CreateAsync(filePath);
        var parquetColumns = await reader.ReadEntireRowGroupAsync();
        var araColumns = parquetColumns.Select((c, i) => new ParquetColumnAdapter(c, i)).ToList();
        return new ReadOnlyDataTable(name, araColumns);
    }

    public static async Task<IDataTable> ReadParquetAsync(this Stream stream, string name)
    {
        var reader = await ParquetReader.CreateAsync(stream);
        var parquetColumns = await reader.ReadEntireRowGroupAsync();
        var araColumns = parquetColumns.Select((c, i) => new ParquetColumnAdapter(c, i)).ToList();
        return new ReadOnlyDataTable(name, araColumns);
    }

    /// <summary>
    /// Reads every "*.parquet" entry from <paramref name="zipPath"/>
    /// and returns them as a list of tables.
    /// </summary>
    public static async Task<IDataSet> ReadParquetFromZipAsync(this FilePath zipPath)
    {
        var tables = new List<IDataTable>();

        await using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in zip.Entries
                     .Where(e => e.Name.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(e => e.FullName))
        {
            await using var entryStream = entry.Open();
            await using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms);

            ms.Position = 0;
            var table = await ReadParquetAsync(ms, Path.GetFileNameWithoutExtension(entry.Name));
            tables.Add(table);
        }

        return tables.ToDataSet();
    }

    public static IDataSet ReadParquetFromZip(this FilePath filePath)
        => Task.Run(filePath.ReadParquetFromZipAsync).GetAwaiter().GetResult();

    public class ParquetColumnAdapter : IDataColumn
    {
        public DataColumn Column;

        public ParquetColumnAdapter(DataColumn dc, int index)
        {
            Column = dc;
            ColumnIndex = index;
            Descriptor = new DataDescriptor(dc.Field.Name, dc.Field.ClrType, index);
            Count = Column.NumValues;
        }

        public int ColumnIndex { get; }
        public IDataDescriptor Descriptor { get; }
        public int Count { get; }
        public object this[int n] => Column.Data.GetValue(n);
        public Array AsArray() => Column.Data;
    }


    public static void WriteParquetToZip(this BimGeometry bg, FilePath file,
        CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli,
        CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
        CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
        => Task.Run(() => WriteParquetToZipAsync(bg, file, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel)).GetAwaiter().GetResult();

    public static async Task WriteParquetToZipAsync(this BimGeometry bg, FilePath file, 
        CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli, 
        CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
        CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
    {
        await using var fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);
        await WriteParquetToZipAsync(bg, zip, parquetCompressionMethod, parquetCompressionLevel);
    }

    public static void WriteParquetToZip(this BimGeometry bg, ZipArchive zip,
        CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli,
        CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
        CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
        => Task.Run(() =>
                WriteParquetToZipAsync(bg, zip, parquetCompressionMethod, parquetCompressionLevel, zipCompressionLevel))
            .GetAwaiter().GetResult();

    public static async Task WriteParquetToZipAsync(this BimGeometry bg, ZipArchive zip,
        CompressionMethod parquetCompressionMethod = CompressionMethod.Brotli,
        CompressionLevel parquetCompressionLevel = CompressionLevel.Optimal,
        CompressionLevel zipCompressionLevel = CompressionLevel.NoCompression)
    {
        var builders = bg.ToParquet();
        foreach (var builder in builders)
        {
            var entryName = $"{builder.Name}.parquet";
            // Quickly compress data
            var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
            await using var parquetBuffer = new MemoryStream();
            await builder.SaveToStream(parquetBuffer, parquetCompressionMethod, parquetCompressionLevel);
            parquetBuffer.Position = 0;
            await using var entryStream = entry.Open();
            await parquetBuffer.CopyToAsync(entryStream);
        }
    }

    public static List<ParquetBuilder> ToParquet(this BimGeometry bg)
    {
        var r = new List<ParquetBuilder>();
        {
            var pb = new ParquetBuilder("Material");
            pb.Add(bg.MaterialRed, nameof(bg.MaterialRed));
            pb.Add(bg.MaterialGreen, nameof(bg.MaterialGreen));
            pb.Add(bg.MaterialBlue, nameof(bg.MaterialBlue));
            pb.Add(bg.MaterialAlpha, nameof(bg.MaterialAlpha));
            pb.Add(bg.MaterialMetallic, nameof(bg.MaterialRoughness));
            pb.Add(bg.MaterialRoughness, nameof(bg.MaterialMetallic));
            r.Add(pb);
        }
        {
            var pb = new ParquetBuilder("Vertex");
            pb.Add(bg.VertexX, nameof(bg.VertexX));
            pb.Add(bg.VertexY, nameof(bg.VertexY));
            pb.Add(bg.VertexZ, nameof(bg.VertexZ));
            r.Add(pb);
        }
        {
            var pb = new ParquetBuilder("Index");
            pb.Add(bg.IndexBuffer, nameof(bg.IndexBuffer));
            r.Add(pb);
        }
        {
            var pb = new ParquetBuilder("Element");
            pb.Add(bg.ElementMaterialIndex, nameof(bg.ElementMaterialIndex));
            pb.Add(bg.ElementMeshIndex, nameof(bg.ElementMeshIndex));
            pb.Add(bg.ElementTransformIndex, nameof(bg.ElementTransformIndex));
            r.Add(pb);
        }
        {
            var pb = new ParquetBuilder("Mesh");
            pb.Add(bg.MeshIndexOffset, nameof(bg.MeshIndexOffset));
            pb.Add(bg.MeshVertexOffset, nameof(bg.MeshVertexOffset));
            r.Add(pb);
        }
        {
            var pb = new ParquetBuilder("Transform");
            pb.Add(bg.TransformTX, nameof(bg.TransformTX));
            pb.Add(bg.TransformTY, nameof(bg.TransformTY));
            pb.Add(bg.TransformTZ, nameof(bg.TransformTZ));
            pb.Add(bg.TransformQX, nameof(bg.TransformQX));
            pb.Add(bg.TransformQY, nameof(bg.TransformQY));
            pb.Add(bg.TransformQZ, nameof(bg.TransformQZ));
            pb.Add(bg.TransformQW, nameof(bg.TransformQW));
            pb.Add(bg.TransformSX, nameof(bg.TransformSX));
            pb.Add(bg.TransformSY, nameof(bg.TransformSY));
            pb.Add(bg.TransformSZ, nameof(bg.TransformSZ));
            r.Add(pb);
        }
        return r;
    }

    public static async Task<BimGeometry> ReadBimGeometryFromParquetZipAsync(this FilePath fp)
        => (await fp.ReadParquetFromZipAsync()).ToBimGeometry();

    public static BimGeometry ReadBimGeometryFromParquetZip(this FilePath fp)
        => Task.Run(fp.ReadBimGeometryFromParquetZipAsync).GetAwaiter().GetResult();
}