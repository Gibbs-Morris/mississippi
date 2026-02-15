namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisValidationMessage" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define validation UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisValidationMessageViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisValidationMessageViewModel Default { get; } = new();

    /// <summary>
    ///     Gets an optional additional CSS class for the validation message.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id of the form element this message is associated with.
    /// </summary>
    public string? For { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the severity of the validation message.
    /// </summary>
    public MisValidationMessageSeverity Severity { get; init; } = MisValidationMessageSeverity.Error;
}
