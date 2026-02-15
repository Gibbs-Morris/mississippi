namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Defines the severity levels for <see cref="MisValidationMessage" />.
/// </summary>
public enum MisValidationMessageSeverity
{
    /// <summary>
    ///     Indicates an error that must be resolved.
    /// </summary>
    Error,

    /// <summary>
    ///     Indicates a warning that may need attention.
    /// </summary>
    Warning,

    /// <summary>
    ///     Indicates an informational message.
    /// </summary>
    Info,

    /// <summary>
    ///     Indicates a successful validation.
    /// </summary>
    Success,
}