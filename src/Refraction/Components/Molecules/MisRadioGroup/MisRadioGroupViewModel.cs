using System.Collections.Generic;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisRadioGroup" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define radio group UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisRadioGroupViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisRadioGroupViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the radio group.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the radio group.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the radio group element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the radio group is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the radio group is required for form submission.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the available selectable options.
    /// </summary>
    public IReadOnlyList<MisRadioOptionViewModel> Options { get; init; } = [];

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisRadioGroupState State { get; init; } = MisRadioGroupState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the selected value.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
