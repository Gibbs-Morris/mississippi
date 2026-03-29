using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Organisms;

/// <summary>
///     Renders a titled content surface with optional footer content.
/// </summary>
public sealed partial class SurfacePanel : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the main content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the footer content.
    /// </summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    ///     Gets or sets the current component state.
    /// </summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    /// <summary>
    ///     Gets or sets the panel title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(AdditionalCssClass)
            ? "rf-surface-panel"
            : $"rf-surface-panel {AdditionalCssClass}";

    private string? AdditionalCssClass =>
        AdditionalAttributes is not null &&
        AdditionalAttributes.TryGetValue("class", out object? classValue)
            ? classValue?.ToString()
            : null;

    private IReadOnlyDictionary<string, object>? UnmatchedAttributes =>
        AdditionalAttributes?
            .Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}