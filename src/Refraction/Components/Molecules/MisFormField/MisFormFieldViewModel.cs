namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisFormField" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define form field UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisFormFieldViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisFormFieldViewModel Default { get; } = new();

    /// <summary>
    ///     Gets an optional additional CSS class for the form field container.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the container element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the field is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisFormFieldState State { get; init; } = MisFormFieldState.Default;
}
