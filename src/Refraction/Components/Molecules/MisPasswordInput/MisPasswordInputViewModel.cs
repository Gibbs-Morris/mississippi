namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisPasswordInput" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define password input UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisPasswordInputViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisPasswordInputViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the password input.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets the autocomplete attribute value.
    /// </summary>
    public string? AutoComplete { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the password input.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the password input element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the password input is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the password is currently visible.
    /// </summary>
    public bool IsPasswordVisible { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the password input is read-only.
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
    ///     Gets the semantic visual state.
    /// </summary>
    public MisPasswordInputState State { get; init; } = MisPasswordInputState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the current password value.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
