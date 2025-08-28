using System;
using Ara3D.Utils;

namespace Ara3D.ScriptService;

public class ScriptType
{
    public ScriptType(Type type, FilePath source)
    {
        Type = type;
        Source = source;
            
        if (HasDefaultCtor)
        {
            try
            {
                DefaultValue = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }

    public bool HasDefaultCtor => Type.HasDefaultConstructor();
    public string ErrorMessage { get; }
    public object DefaultValue { get; }
    public Type Type { get; }
    public FilePath Source { get; }
}