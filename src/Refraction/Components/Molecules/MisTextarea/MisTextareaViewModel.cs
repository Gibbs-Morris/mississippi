namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisTextarea" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define textarea UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisTextareaViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisTextareaViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the textarea.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the textarea.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the textarea element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the textarea is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the textarea is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the textarea is required for form submission.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the optional placeholder text shown when value is empty.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    ///     Gets the number of visible text lines.
    /// </summary>
    public int Rows { get; init; } = 4;

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisTextareaState State { get; init; } = MisTextareaState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the current textarea value.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
