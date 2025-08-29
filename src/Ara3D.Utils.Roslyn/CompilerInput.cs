using System.Collections.Generic;

namespace Ara3D.Utils.Roslyn;

public class CompilerInput
{
    public IReadOnlyList<FilePath> InputFiles { get; }
    public CompilerOptions Options { get; }
    public IReadOnlyList<FilePath> Refs { get; }

    public CompilerInput(IReadOnlyList<FilePath> inputFiles, CompilerOptions options, IReadOnlyList<FilePath> refs)
    {
        InputFiles = inputFiles;
        Options = options;
        Refs = refs;
    }
}