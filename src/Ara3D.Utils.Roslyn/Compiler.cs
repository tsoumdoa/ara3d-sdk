using Ara3D.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ara3D.Utils.Roslyn
{
    /// <summary>
    /// Used for a single compilation event.
    /// This uses a cache to minimize recompilation 
    /// </summary>
    public class Compiler
    {
        public ILogger Logger { get; }
        public CompilerInput Input { get; }
        public CompilerOutput Output { get; }

        public FilePath OutputFile => Input.Options.OutputFile;
        public FilePath CacheFilePath => OutputFile.RelativeFile("cache.json");
        public CompilerOptions Options => Input.Options;
        public IReadOnlyList<FilePath> InputFiles => Input.InputFiles;

        public Compiler(
            CompilerInput input,
            ILogger logger, 
            CancellationToken token)
        {
            Input = input;
            Logger = logger;

            Log(".:Consulting Cache:.");
            Output = TryLoadCache();
            if (Output?.Success == true)
                return;

            Log(".:Parsing:.");
            if (token.IsCancellationRequested) return;
            var parsedInput = new ParsedCompilerInput(input, token);

            Log(".:Compiling:.");
            if (token.IsCancellationRequested) return;
            var compilation = parsedInput.CompileCSharpStandard(null, token);

            Log($".:Writing Cache:.");
            var result = SerializableCompilationResult.Create(compilation);
            CacheFilePath.WriteJson(result);
            Output = new CompilerOutput(result);

            Log($".:Diagnostics:.");
            foreach (var x in Output.Result.Diagnostics)
                Log($"  {x}");

            Log(Output.Success ? ".:Compilation Succeeded:." : ".:Compilation Failed:.");
        }

        public CompilerOutput TryLoadCache()
        {
            if (Options.UseCache && CacheFilePath.Exists())
            {
                try
                {
                    Logger.Log("Consulting cache file");
                    var cache = LoadCache();
                    if (cache.IsValid(InputFiles))
                    {
                        Logger.Log("Cache is valid.");
                        Logger.Log($"Attempting to load assembly from {OutputFile}");
                        var r = new CompilerOutput(cache);
                        if (r.Success)
                            return r;
                        Logger.Log($"Failed to load assembly: {r.ErrorMessage}");
                    }
                    else
                    {
                        Logger.Log("Cache is invalid.");
                    }

                    Logger.Log("Failed to load cache, deleting the cache file.");
                    CacheFilePath.Delete();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }

            return null;
        }

        public SerializableCompilationResult LoadCache()
            => CacheFilePath.LoadJson<SerializableCompilationResult>();

        public void Log(string s)
            => Logger?.Log(s);
    }
}