namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisCheckbox" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define checkbox UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisCheckboxViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisCheckboxViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the checkbox.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the checkbox.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the checkbox element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the checkbox is checked.
    /// </summary>
    public bool IsChecked { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the checkbox is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the checkbox is required for form submission.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisCheckboxState State { get; init; } = MisCheckboxState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the submitted value attribute.
    /// </summary>
    public string Value { get; init; } = "true";
}
