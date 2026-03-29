using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Client.Components.Atoms;

/// <summary>
///     Renders a presentational command trigger that reports user intent via an event callback.
/// </summary>
public sealed partial class CommandButton : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the command button is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    ///     Gets or sets the button label.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the callback invoked when the button is pressed.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

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
        string.IsNullOrWhiteSpace(AdditionalCssClass) ? "rf-command-button" : $"rf-command-button {AdditionalCssClass}";

    private IReadOnlyDictionary<string, object>? UnmatchedAttributes =>
        AdditionalAttributes
            ?.Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);

    private Task HandleClickAsync(
        MouseEventArgs args
    )
    {
        _ = args;
        return OnClick.InvokeAsync();
    }
}