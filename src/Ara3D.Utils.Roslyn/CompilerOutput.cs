using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ara3D.Utils.Roslyn;

public class CompilerOutput
{
    public bool Success => Assembly != null;
    public Assembly Assembly { get; }
    public string ErrorMessage { get; }
    public FilePath OutputFile => Result.OutputFile;
    public SerializableCompilationResult Result { get; }
    public IReadOnlyList<Type> ExportedTypes => Assembly?.ExportedTypes.ToList() ?? [];

    public CompilerOutput(SerializableCompilationResult result)
    {
        Result = result;
        if (OutputFile.Value.IsNullOrWhiteSpace())
            ErrorMessage = "Output file is empty";
        else if (!OutputFile.Exists())
            ErrorMessage = $"Output file not found: {OutputFile}";
        else
        {
            try
            {
                Assembly = Assembly.LoadFile(OutputFile);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}