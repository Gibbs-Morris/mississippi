// <copyright file="MessageComposer.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Molecules;

/// <summary>
///     Molecule component for composing and sending messages.
/// </summary>
public sealed partial class MessageComposer : ComponentBase
{
    private string messageContent = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether a message is currently being sent.
    /// </summary>
    [Parameter]
    public bool IsSending { get; set; }

    /// <summary>
    ///     Gets or sets the callback when a message should be sent. Commands flow UP via EventCallback.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public EventCallback<string> OnSend { get; set; }

    private async Task HandleSendAsync()
    {
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            return;
        }

        string content = messageContent;
        messageContent = string.Empty; // Clear for UX (optimistic update)

        // Raise event to parent - parent dispatches command to aggregate
        await OnSend.InvokeAsync(content);
    }
}