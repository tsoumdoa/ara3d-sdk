using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ara3D.Utils.Roslyn
{
    public class ParsedCompilerInput
    {
        public IReadOnlyList<ParsedSourceFile> ParsedSourceFiles { get; }
        public bool HasParseErrors => ParsedSourceFiles.Any(sf => !sf.Success);
        public IEnumerable<SyntaxTree> SyntaxTrees => ParsedSourceFiles.Select(sf => sf.SyntaxTree);
        public IEnumerable<EmbeddedText> EmbeddedTexts => ParsedSourceFiles.Select(sf => sf.EmbeddedText);
        public CompilerInput RawInput { get; }
        public CompilerOptions Options => RawInput.Options;

        public ParsedCompilerInput(CompilerInput rawInput, CancellationToken token)
        {
            RawInput = rawInput;
            ParsedSourceFiles = rawInput.InputFiles.ParseCSharp(Options, token).AsParallel().ToList();
        }
    }
}