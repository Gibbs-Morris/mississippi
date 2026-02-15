namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Defines the visual states for <see cref="MisPasswordInput" />.
/// </summary>
public enum MisPasswordInputState
{
    /// <summary>
    ///     Default appearance.
    /// </summary>
    Default,

    /// <summary>
    ///     Indicates the input has an error.
    /// </summary>
    Error,

    /// <summary>
    ///     Indicates the input has a warning.
    /// </summary>
    Warning,

    /// <summary>
    ///     Indicates a successful or valid state.
    /// </summary>
    Success,
}