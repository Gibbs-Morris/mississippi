namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Defines the visual states for <see cref="MisLabel" />.
/// </summary>
public enum MisLabelState
{
    /// <summary>
    ///     Default appearance.
    /// </summary>
    Default,

    /// <summary>
    ///     Indicates the associated field has an error.
    /// </summary>
    Error,

    /// <summary>
    ///     Indicates the associated field has a warning.
    /// </summary>
    Warning,

    /// <summary>
    ///     Indicates the field is disabled.
    /// </summary>
    Disabled,
}
