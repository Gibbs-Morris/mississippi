namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisButton" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define button UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisButtonViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisButtonViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the button.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the button.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the button is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the HTML button type attribute.
    /// </summary>
    public MisButtonType Type { get; init; } = MisButtonType.Button;
}