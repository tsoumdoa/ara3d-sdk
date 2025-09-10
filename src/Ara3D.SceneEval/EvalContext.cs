using Ara3D.Logging;

namespace Ara3D.SceneEval;

public class EvalContext
{
    public DateTime Started = DateTime.Now;
    public double AnimationTime { get; }
    public ILogger Logger { get; }

    public EvalContext(double animationTime, ILogger logger)
    {
        AnimationTime = animationTime;
        Logger = logger;
    }
}