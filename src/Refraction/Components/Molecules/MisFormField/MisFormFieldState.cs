namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Defines the visual states for <see cref="MisFormField" />.
/// </summary>
public enum MisFormFieldState
{
    /// <summary>
    ///     Default appearance.
    /// </summary>
    Default,

    /// <summary>
    ///     Indicates the field has an error.
    /// </summary>
    Error,

    /// <summary>
    ///     Indicates the field has a warning.
    /// </summary>
    Warning,

    /// <summary>
    ///     Indicates a successful or valid state.
    /// </summary>
    Success,
}