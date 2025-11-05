namespace Ara3D.Studio.API;

[AttributeUsage(AttributeTargets.Class)]
public class OnDemandAttribute : Attribute
{
    public OnDemandAttribute() {}
}

[AttributeUsage(AttributeTargets.Class)]
public class AnimatedAttribute : Attribute
{
    public AnimatedAttribute() { }
}

