using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IAnimatedModelGenerator : IScriptedCommand, IAnimated
{
    Model3D Eval(EvalContext context);
}