using Ara3D.Logging;
using Ara3D.Models;
using Ara3D.Utils;

namespace Ara3D.Studio.API;

public interface IModelAsset : IAsset
{
    string Name { get; }
    FilePath FilePath { get; }
    Task<IModel3D> Import(ILogger logger);
    IModel3D Eval(EvalContext context);
}