using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ara3D.Logging;
using Ara3D.Services;
using Ara3D.Utils;
using Ara3D.Utils.Roslyn;
using Microsoft.CodeAnalysis;
using Compilation = Ara3D.Utils.Roslyn.Compilation;

namespace Ara3D.ScriptService
{
    public class ScriptingService : 
        SingletonModelBackedService<ScriptingDataModel>, 
        IScriptingService
    {
        public Compiler Compiler => WatchingCompiler?.Compiler;
        public DirectoryWatchingCompiler WatchingCompiler { get; }
        public ILogger Logger { get; set; }
        public ScriptingOptions Options { get; }
        public Assembly Assembly => WatchingCompiler?.Compiler?.Assembly;
        public IReadOnlyList<ScriptType> Types { get; private set; } = [];

        public ScriptingService(IServiceManager app, ILogger logger, ScriptingOptions options)
            : base(app)
        {
            Logger = logger ?? new Logger(LogWriter.DebugWriter, "Scripting Service");
            Options = options;
            CreateInitialFolders();
            WatchingCompiler = new DirectoryWatchingCompiler(Logger, Options.ScriptsFolder, Options.LibrariesFolder);
            WatchingCompiler.RecompileEvent += WatchingCompilerRecompileEvent;
            UpdateDataModel();
        }

        public void ExecuteCommand(IScriptedCommand command)
        {
            try
            {
                Logger.Log($"Starting command execution: {command.Name}");
                command.Execute();
                Logger.Log($"Finished command execution: {command.Name}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Command execution failed: {e}");
            }
        }

        public void Compile()
        {
            WatchingCompiler.Compile();
        }

        public override void Dispose()
        {
            base.Dispose();
            WatchingCompiler.Dispose();
        }

        private void WatchingCompilerRecompileEvent(object sender, EventArgs e)
        {
            UpdateDataModel();
        }

        public void CreateInitialFolders()
        {
            Options.ScriptsFolder.Create();
            Options.LibrariesFolder.Create();
        }

        public bool AutoRecompile
        {
            get => WatchingCompiler.AutoRecompile;
            set => WatchingCompiler.AutoRecompile = value;
        }

        public static Dictionary<string, FilePath> BuildTypeToFileMap(Compilation compilation)
        {
            var result = new Dictionary<string, FilePath>(StringComparer.Ordinal);

            if (compilation == null)
                return result;

            void Visit(INamespaceSymbol ns)
            {
                foreach (var t in ns.GetTypeMembers())
                    AddTypeAndPartials(t);
                foreach (var child in ns.GetNamespaceMembers())
                    Visit(child);
            }

            void AddTypeAndPartials(INamedTypeSymbol type)
            {
                // One type may have multiple declaring syntax locations (partials)
                var paths = type.DeclaringSyntaxReferences
                    .Select(r => r.SyntaxTree.FilePath)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .ToArray();

                if (paths.Length > 0)
                    result[type.ToDisplayString()] = paths[0]; // or store all paths if you prefer

                foreach (var nested in type.GetTypeMembers())
                    AddTypeAndPartials(nested);
            }

            Visit(compilation.Compiler.GlobalNamespace);
            return result;
        }

        public void UpdateDataModel()
        {
            var typeNameToFilePath = BuildTypeToFileMap(Compiler?.Compilation);

            // Create "ScriptTypes" which have a default value 
            var types = Compiler?.ExportedTypes.ToArray() ?? [];
            var scriptTypes = new List<ScriptType>();
            foreach (var type in types)
            {
                var path = typeNameToFilePath.GetValueOrDefault(type.FullName ?? "");
                scriptTypes.Add(new ScriptType(type, path));
            }
            Types = scriptTypes;

            Repository.Value = new ScriptingDataModel()
            {
                Dll = Assembly?.Location ?? "",
                Directory = WatchingCompiler?.Directory,
                TypeNames = Types.Select(t => t.Type.FullName).OrderBy(t => t).ToArray(),
                Files = Compiler?.Input?.SourceFiles?.Select(sf => sf.FilePath).OrderBy(x => x.Value).ToArray() ?? [],
                Assemblies = Compiler?.Refs?.Select(fp => fp.Value).ToList(),
                Diagnostics = Compiler?.Compilation?.Diagnostics?.Select(d => d.ToString()).ToArray() ?? [],
                ParseSuccess = Compiler?.Input?.HasParseErrors == false,
                EmitSuccess = Compiler?.CompilationSuccess == true,
                LoadSuccess = Assembly != null,
                Options = Options,
            };
        }

    }
}