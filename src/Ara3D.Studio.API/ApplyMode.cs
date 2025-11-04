namespace Ara3D.Studio.API;

public enum ApplyMode
{
    /// <summary>
    /// Modifier is recomputed whenever a value changes. 
    /// </summary>
    Dynamic,

    /// <summary>
    /// Modifier has an "Apply" button and is recomputed only when the Apply button is pressed. 
    /// </summary>
    OnDemand,

    /// <summary>
    /// Default mode is dynamic.
    /// </summary>
    Default = Dynamic,
}