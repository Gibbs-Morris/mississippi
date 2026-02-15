using System.Collections.Generic;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisCheckboxGroup" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define checkbox group UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisCheckboxGroupViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisCheckboxGroupViewModel Default { get; } = new();

    /// <summary>
    ///     Gets the accessible label of the checkbox group.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets an optional additional CSS class for the checkbox group.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the checkbox group element.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    ///     Gets the intent identifier emitted back to parent state handlers.
    /// </summary>
    public string IntentId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the checkbox group is disabled.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    ///     Gets a value indicating whether at least one checkbox must be selected.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     Gets the name attribute value for form binding.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Gets the available selectable options.
    /// </summary>
    public IReadOnlyList<MisCheckboxOptionViewModel> Options { get; init; } = [];

    /// <summary>
    ///     Gets the semantic visual state.
    /// </summary>
    public MisCheckboxGroupState State { get; init; } = MisCheckboxGroupState.Default;

    /// <summary>
    ///     Gets the title attribute shown as a browser tooltip.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the selected values.
    /// </summary>
    public IReadOnlySet<string> Values { get; init; } = new HashSet<string>();
}