using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IModelModifier : IScriptedComponent
{
    IModel3D Eval(IModel3D model3D, EvalContext context);
}