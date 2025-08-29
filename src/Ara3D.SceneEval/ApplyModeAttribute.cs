namespace Ara3D.SceneEval;

/// <summary>
/// Use on your modifiers to determine if they get reloaded. 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApplyModeAttribute : Attribute
{
    public ApplyMode Mode { get; }
    public ApplyModeAttribute(ApplyMode mode) => Mode = mode;
}