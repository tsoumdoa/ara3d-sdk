using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ara3D.Utils.Roslyn
{
    public class CompilerOptions
    {
        public CompilerOptions(IEnumerable<FilePath> fileReferences, FilePath outputFileName, bool debug, bool useCache)
            => (FileReferences, OutputFile, Debug, UseCache) 
                = (fileReferences.ToList(), outputFileName, debug, useCache);

        public FilePath OutputFile { get; }
        public bool Debug { get; }
        public bool UseCache { get; }
        public IReadOnlyList<FilePath> FileReferences { get; }

        public string AssemblyName
            => OutputFile.GetFileNameWithoutExtension();

        public LanguageVersion Language
            => LanguageVersion.CSharp12;

        public CSharpParseOptions ParseOptions => new(Language);

        public CSharpCompilationOptions CompilationOptions
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)                
                .WithOptimizationLevel(Debug ? OptimizationLevel.Debug : OptimizationLevel.Release);
    
        public IEnumerable<MetadataReference> MetadataReferences
            => RoslynUtils.ReferencesFromFiles(FileReferences);

        public CompilerOptions WithNewOutputFilePath(string fileName = null) =>
            new(FileReferences, fileName, Debug, UseCache);

        public CompilerOptions WithNewReferences(IEnumerable<FilePath> fileReferences) =>
            new(fileReferences, OutputFile, Debug, UseCache);

        public static CompilerOptions CreateDefault()
            => new(RoslynUtils.LoadedAssemblyLocations(), 
                RoslynUtils.GenerateNewDllFileName(), true, false);

        public CompilerOptions WithCaching()
            => new(FileReferences, OutputFile, Debug, true);

        public static CompilerOptions CreateDefault(Type[] types)
            => new(types.Select(t => (FilePath)t.Assembly.Location),
                RoslynUtils.GenerateNewDllFileName(), true, false);
    }
}
