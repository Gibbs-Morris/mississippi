using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Cascade.Client.Components.Atoms.Input;

/// <summary>
///     Atomic component for text input fields with label and error state support.
/// </summary>
public sealed partial class Input : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the input.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the error message to display.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the input label text.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when Enter key is pressed.
    /// </summary>
    [Parameter]
    public EventCallback OnEnter { get; set; }

    /// <summary>
    ///     Gets or sets the placeholder text.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    ///     Gets or sets the input type. Valid values: text, password, email.
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "text";

    /// <summary>
    ///     Gets or sets the current input value.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the callback when the value changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    private bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private string InputId { get; } = $"input-{Guid.NewGuid():N}"[..16];

    private Task HandleInputAsync(
        ChangeEventArgs e
    ) =>
        ValueChanged.InvokeAsync(e.Value?.ToString() ?? string.Empty);

    private Task HandleKeyPressAsync(
        KeyboardEventArgs e
    ) =>
        e.Key == "Enter" ? OnEnter.InvokeAsync() : Task.CompletedTask;
}