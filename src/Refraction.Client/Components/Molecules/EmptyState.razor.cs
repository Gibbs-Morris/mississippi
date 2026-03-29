using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Molecules;

/// <summary>
///     Renders a presentational empty-state surface that explains why a work area has no current content.
/// </summary>
public sealed partial class EmptyState : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the supporting empty-state message.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current component state.
    /// </summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Quiet;

    /// <summary>
    ///     Gets or sets the optional empty-state title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    private string? AdditionalCssClass =>
        AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out object? classValue)
            ? classValue?.ToString()
            : null;

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(AdditionalCssClass) ? "rf-empty-state" : $"rf-empty-state {AdditionalCssClass}";

    private IReadOnlyDictionary<string, object>? UnmatchedAttributes =>
        AdditionalAttributes
            ?.Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}