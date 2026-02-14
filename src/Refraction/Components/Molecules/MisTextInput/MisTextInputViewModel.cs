namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisTextInput" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define text input UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisTextInputViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisTextInputViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the autocomplete attribute value.
    /// </summary>
    public string? AutoComplete { get; init; }

    /// <summary>
    ///     Gets the accessible label of the text input.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the text input.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the text input element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the text input is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the text input is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the placeholder text shown when no value is present.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the text input type.
    /// </summary>
    public MisTextInputType Type { get; init; } = MisTextInputType.Text;

    /// <summary>
    ///     Gets the current text input value.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}