using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IModelGenerator : IScriptedComponent
{
    Model3D Eval(EvalContext context);
}