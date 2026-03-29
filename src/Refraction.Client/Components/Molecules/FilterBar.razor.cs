using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Molecules;

/// <summary>
///     Renders a presentational filter bar that groups enterprise filter controls.
/// </summary>
public sealed partial class FilterBar : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? AdditionalCssClass =>
        AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out object? classValue)
            ? classValue?.ToString()
            : null;

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(AdditionalCssClass) ? "rf-filter-bar" : $"rf-filter-bar {AdditionalCssClass}";

    private IReadOnlyDictionary<string, object>? UnmatchedAttributes =>
        AdditionalAttributes
            ?.Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}