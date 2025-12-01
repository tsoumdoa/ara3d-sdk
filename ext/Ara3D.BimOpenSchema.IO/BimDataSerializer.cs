using Ara3D.Utils;

namespace Ara3D.BimOpenSchema.IO;

public static class BimDataSerializer
{
    public static void WriteDuckDB(this BimData data, FilePath fp)
        => data.ToDataSet().WriteToDuckDB(fp);

    public static void WriteToExcel(this BimData data, FilePath fp)
        => data.ToDataSet().WriteToExcel(fp);
}