using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Atoms;

/// <summary>
///     Renders a compact presentational badge for concise status labels.
/// </summary>
public sealed partial class StatusBadge : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the badge label.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current component state.
    /// </summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    private string? AdditionalCssClass =>
        AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out object? classValue)
            ? classValue?.ToString()
            : null;

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(AdditionalCssClass) ? "rf-status-badge" : $"rf-status-badge {AdditionalCssClass}";

    private IReadOnlyDictionary<string, object>? UnmatchedAttributes =>
        AdditionalAttributes
            ?.Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}