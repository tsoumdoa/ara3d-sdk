using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IAnimatedModelModifier : IScriptedComponent, IAnimated
{
    Model3D Eval(Model3D model3D, EvalContext context);
}