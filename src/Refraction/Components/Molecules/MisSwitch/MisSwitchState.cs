namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents semantic visual states supported by <see cref="MisSwitch" />.
/// </summary>
public enum MisSwitchState
{
    /// <summary>
    ///     Uses the default neutral visual style.
    /// </summary>
    Default,

    /// <summary>
    ///     Uses a positive/confirmed visual style.
    /// </summary>
    Success,

    /// <summary>
    ///     Uses a caution visual style.
    /// </summary>
    Warning,

    /// <summary>
    ///     Uses an error visual style.
    /// </summary>
    Error,
}