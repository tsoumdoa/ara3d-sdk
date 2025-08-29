using System.Text.Json;

namespace Ara3D.Utils;

public static class JsonUtil
{
    public static FilePath WriteJson<T>(this FilePath filePath, T self, bool includeFields = false, bool writeIndented = true)
    {
        using var fs = filePath.OpenWrite();
        JsonSerializer.Serialize(fs, self, new JsonSerializerOptions() { IncludeFields = includeFields, WriteIndented = writeIndented } );
        return filePath;
    }

    public static T LoadJson<T>(this FilePath filePath)
    {
        using var fs = filePath.OpenRead();
        return JsonSerializer.Deserialize<T>(fs);
    }
}