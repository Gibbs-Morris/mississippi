using System.Collections.Generic;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisSelect" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define select UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisSelectViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisSelectViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the select.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the select.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the select element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the select is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the select is required for form submission.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the available selectable options.
    /// </summary>
    public IReadOnlyList<MisSelectOptionViewModel> Options { get; init; } = [];

    /// <summary>
    ///     Gets the optional placeholder text shown as the empty selection option.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisSelectState State { get; init; } = MisSelectState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the currently selected value.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}