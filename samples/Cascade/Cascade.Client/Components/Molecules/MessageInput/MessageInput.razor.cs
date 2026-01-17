using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Cascade.Client.Components.Molecules.MessageInput;

/// <summary>
///     Molecule component combining a text input field with a send button for chat messages.
/// </summary>
public sealed partial class MessageInput : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether the input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when a message is submitted.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSend { get; set; }

    /// <summary>
    ///     Gets or sets the placeholder text.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "Type a message...";

    private string CurrentMessage { get; set; } = string.Empty;

    private bool ShouldPreventDefault { get; set; }

    private async Task HandleKeyDownAsync(
        KeyboardEventArgs e
    )
    {
        // Prevent Enter from inserting a newline (unless Shift is held)
        ShouldPreventDefault = (e.Key == "Enter") && !e.ShiftKey;
        if (ShouldPreventDefault)
        {
            await HandleSendAsync();
        }
    }

    private async Task HandleSendAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage))
        {
            return;
        }

        string message = CurrentMessage.Trim();
        CurrentMessage = string.Empty;
        await OnSend.InvokeAsync(message);
    }
}