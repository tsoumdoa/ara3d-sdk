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
        public string Version { get; set; }
        public string Path { get; set; }
        public DateTime Modified { get; set; }
        public long Size { get; set; }

        public static CachedFileStats Create(string version, FilePath filePath)
        {
            return new CachedFileStats
            {
                Version = version,
                Path = filePath.GetFullPath(),
                Modified = filePath.GetModifiedTime(),
                Size = filePath.GetFileSize()
            };
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        public override bool Equals(object other)
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
            => other is CachedFileStats fs && Equals(fs);

        public bool Equals(CachedFileStats other)
            => other != null && Path == other.Path && Modified == other.Modified && Size == other.Size && Version == other.Version;
    }

    public static bool Compare(IReadOnlyList<CachedFileStats> filesA, IReadOnlyList<CachedFileStats> filesB)
        => filesA.SequenceEqual(filesB);

    public bool IsValid(string version, IEnumerable<FilePath> inputFiles)
        => Compare(InputFiles, CreateCachedFileStats(version, inputFiles)) && Path.Exists(OutputFile);

    public static List<CachedFileStats> CreateCachedFileStats(string version, IEnumerable<FilePath> files)
        => files.Select(f => CachedFileStats.Create(version, f)).ToList();

    public static SerializableCompilationResult Create(Compilation compilation, string version)
        => new()
        {
            Diagnostics = compilation.Diagnostics.ToList(),
            InputFiles = CreateCachedFileStats(version, compilation.InputFiles),
            OutputFile = compilation.OutputFilePath,
            TypeToSourceMap = compilation.GetTypeMap()
        };
}