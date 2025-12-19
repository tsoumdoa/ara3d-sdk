using Ara3D.Utils;

namespace Ara3D.BimOpenSchema.IO;

public static class BimDataSerializer
{
    public static void WriteToExcel(this IBimData data, FilePath fp)
        => data.ToDataSet().WriteToExcel(fp);
}