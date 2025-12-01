using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IModelGenerator : IScriptedComponent
{
    IModel3D Eval(EvalContext context);
}