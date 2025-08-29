using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ara3D.Utils.Roslyn;

public class SerializableCompilationResult
{
    public string OutputFile { get; set; }
    public List<string> Diagnostics { get; set; }
    public List<CachedFileStats> InputFiles { get; set; }
    public Dictionary<string, string> TypeToSourceMap { get; set; }

    public class CachedFileStats : IEquatable<CachedFileStats>
    {
        public string Path { get; set; }
        public DateTime Modified { get; set; }
        public long Size { get; set; }

        public static CachedFileStats Create(FilePath filePath)
        {
            return new CachedFileStats
            {
                Path = filePath.GetFullPath(),
                Modified = filePath.GetModifiedTime(),
                Size = filePath.GetFileSize()
            };
        }

        public override bool Equals(object other)
            => other is CachedFileStats fs && Equals(fs);

        public bool Equals(CachedFileStats other)
            => other != null && Path == other.Path && Modified == other.Modified && Size == other.Size;
    }

    public static bool Compare(IReadOnlyList<CachedFileStats> filesA, IReadOnlyList<CachedFileStats> filesB)
        => filesA.SequenceEqual(filesB);

    public bool IsValid(IEnumerable<FilePath> inputFiles)
        => Compare(InputFiles, CreateCachedFileStats(inputFiles)) && Path.Exists(OutputFile);

    public static List<CachedFileStats> CreateCachedFileStats(IEnumerable<FilePath> files)
        => files.Select(CachedFileStats.Create).ToList();

    public static SerializableCompilationResult Create(Compilation compilation)
        => new()
        {
            Diagnostics = compilation.Diagnostics.ToList(),
            InputFiles = CreateCachedFileStats(compilation.InputFiles),
            OutputFile = compilation.OutputFilePath,
            TypeToSourceMap = compilation.GetTypeMap()
        };
}