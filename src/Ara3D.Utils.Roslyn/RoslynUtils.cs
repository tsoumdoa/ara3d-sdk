using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Ara3D.Utils.Roslyn
{
    public static partial class RoslynUtils
    {
        public static IEnumerable<MetadataReference> ReferencesFromFiles(IEnumerable<FilePath> files)
            => files.Distinct().Where(fp => fp.Exists()).Select(x => MetadataReference.CreateFromFile(x));

        public static IEnumerable<MetadataReference> ReferencesFromAssemblies(IEnumerable<Assembly> assemblies)
            => ReferencesFromFiles(assemblies.Select(x => (FilePath)x.Location));

        public static IEnumerable<MetadataReference> ReferencesFromTypes(params Type[] types)
            => ReferencesFromAssemblies(types.Select(t => t.Assembly));

        public static IEnumerable<MetadataReference> ReferencesFromLoadedAssemblies()
            => ReferencesFromFiles(LoadedAssemblyLocations());

        public static IEnumerable<FilePath> LoadedAssemblyLocations(AppDomain domain = null)
            => (domain ?? AppDomain.CurrentDomain).GetAssemblies().Where(x => !x.IsDynamic)
                .Select(x => new FilePath(x.Location));

        public static string ToPackageReference(this AssemblyIdentity asm)
            => $"<PackageReference Include=\"{asm.Name}\" Version=\"{asm.Version}\" />";

        public static DirectoryPath GetOrCreateDir(DirectoryPath path)
            => Directory.Exists(path)
                ? path
                : new DirectoryPath(Directory.CreateDirectory(path).FullName);

        public static FilePath GenerateNewDllFileName()
            => PathUtil.CreateTempFile("dll");

        public static FilePath GenerateNewSourceFileName()
            => PathUtil.CreateTempFile("cs");

        public static FilePath WriteToTempFile(string source)
        {
            var path = GenerateNewSourceFileName();
            File.WriteAllText(path, source);
            return path;
        }

        public static Compilation CompileCSharpStandard(this ParsedCompilerInput input,
            CSharpCompilation compilation = null,
            CancellationToken token = default)
        {
            compilation ??= CSharpCompilation.Create(
                input.Options.AssemblyName,
                input.SyntaxTrees,
                input.Options.MetadataReferences,
                input.Options.CompilationOptions);

            var outputPath = input.Options.OutputFile;
            outputPath.DeleteAndCreateDirectory();

            EmitResult emitResult;
            using (var peStream = File.OpenWrite(outputPath))
            {
                var emitOptions = new EmitOptions(false, DebugInformationFormat.Embedded);
                emitResult = compilation.Emit(peStream, null, null, null, null, emitOptions,
                    null, null, input.EmbeddedTexts, token);
            }
            return new Compilation(input, compilation, emitResult);
        }
    }
}