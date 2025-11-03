namespace Ara3D.SceneEval;

/// <summary>
/// Use on your modifiers to determine if they get reloaded. 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApplyModeAttribute : Attribute
{
    public ApplyMode Mode { get; }
    public ApplyModeAttribute(ApplyMode mode) => Mode = mode;
}

[AttributeUsage(AttributeTargets.Class)]
public class ApplyOnDemandAttribute : ApplyModeAttribute
{
    public ApplyOnDemandAttribute() : base(ApplyMode.OnDemand) {}
}