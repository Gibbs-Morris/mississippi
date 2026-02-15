namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisLabel" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define label UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisLabelViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisLabelViewModel Default { get; } = new();

    /// <summary>
    ///     Gets an optional additional CSS class for the label.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id of the form element this label is associated with.
    /// </summary>
    public string? For { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the label element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the associated field is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisLabelState State { get; init; } = MisLabelState.Default;
}