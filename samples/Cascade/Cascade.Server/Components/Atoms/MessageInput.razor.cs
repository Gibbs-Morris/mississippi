// <copyright file="MessageInput.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for text input with enter key support.
/// </summary>
public sealed partial class MessageInput : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether the input is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    ///     Gets or sets the callback when Enter key is pressed.
    /// </summary>
    [Parameter]
    public EventCallback OnEnterPressed { get; set; }

    /// <summary>
    ///     Gets or sets the callback when value changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnValueChanged { get; set; }

    /// <summary>
    ///     Gets or sets the placeholder text.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current input value.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    private async Task HandleInputAsync(
        ChangeEventArgs e
    )
    {
        await OnValueChanged.InvokeAsync(e.Value?.ToString() ?? string.Empty);
    }

    private async Task HandleKeyPressAsync(
        KeyboardEventArgs e
    )
    {
        if (e.Key == "Enter")
        {
            await OnEnterPressed.InvokeAsync();
        }
    }
}