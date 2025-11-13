namespace Ara3D.Studio.API;

public class EvalContext
{
    public IHostApplication Application { get; }
    public double AnimationTime { get; }

    public EvalContext(IHostApplication application, double animationTime)
    {
        Application = application;
        AnimationTime = animationTime;
    }
}