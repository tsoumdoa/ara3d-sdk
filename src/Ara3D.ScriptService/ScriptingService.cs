using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ara3D.Logging;
using Ara3D.Services;
using Ara3D.Utils;
using Ara3D.Utils.Roslyn;

namespace Ara3D.ScriptService;

public class ScriptingService : 
    SingletonModelBackedService<ScriptingDataModel>, 
    IScriptingService
{
    public Compiler Compiler => WatchingCompiler?.Compiler;
    public DirectoryWatchingCompiler WatchingCompiler { get; }
    public ILogger Logger { get; set; }
    public ScriptingOptions Options { get; }
    public Assembly Assembly => WatchingCompiler?.Compiler?.Output?.Assembly;
    public IReadOnlyList<ScriptType> Types { get; private set; } = [];

    public ScriptingService(IServiceManager app, ILogger logger, ScriptingOptions options)
        : base(app)
    {
        Logger = logger ?? new Logger(LogWriter.DebugWriter, "Scripting Service");
        Options = options;
        CreateInitialFolders();
        
        // TODO: decide how we want to handle caching. 
        var compilerOptions = CompilerOptions.CreateDefault().WithCaching();

        WatchingCompiler = new DirectoryWatchingCompiler(Logger, Options.ScriptsFolder, Options.LibrariesFolder, false, compilerOptions);
        WatchingCompiler.RecompileEvent += WatchingCompilerRecompileEvent;
        UpdateDataModel();
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

    public void UpdateDataModel()
    {
        var typeNameToFilePath = Compiler?.Output?.Result?.TypeToSourceMap ?? new Dictionary<string, string>();
        var types = Compiler?.Output?.ExportedTypes.ToArray() ?? [];
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
            Files = Compiler?.InputFiles?.OrderBy(x => x.Value).ToArray() ?? [],
            Assemblies = Compiler?.Input.Refs?.Select(fp => fp.Value).ToList(),
            Diagnostics = Compiler?.Output?.Result?.Diagnostics?.Select(d => d.ToString()).ToArray() ?? [],
            EmitSuccess = Compiler?.Output?.Success == true,
            LoadSuccess = Assembly != null,
            Options = Options,
        };
    }
}