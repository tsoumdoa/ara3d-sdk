using Ara3D.Models;
using Ara3D.SceneEval;

namespace Ara3D.Studio.API;

public interface IModelModifier : IScriptedComponent
{
    Model3D Eval(Model3D model3D, EvalContext context);
}