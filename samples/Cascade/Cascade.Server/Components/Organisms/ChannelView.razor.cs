// <copyright file="ChannelView.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Cascade.Server.Components.Services;
using Cascade.Server.Components.ViewModels;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Organisms;

/// <summary>
///     Organism component that owns state and dispatches commands.
/// </summary>
public sealed partial class ChannelView : ComponentBase
{
    private readonly List<MemberViewModel> members = [];

    private readonly List<MessageViewModel> messages = [];

    private string? errorMessage;

    private bool isLoading = true;

    private bool isSending;

    /// <summary>
    ///     Gets or sets the channel identifier to display.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the channel name to display in the header.
    /// </summary>
    [Parameter]
    public string ChannelName { get; set; } = "Channel";

    [Inject]
    private IChatService ChatService { get; set; } = default!;

    [Inject]
    private UserSession Session { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(ChannelId))
        {
            await LoadChannelDataAsync();
        }
    }

    /// <summary>
    ///     Command handler - receives command from child, dispatches to aggregate.
    ///     This is where "commands flow up" meets "dispatch to aggregate".
    /// </summary>
    private async Task HandleSendMessageAsync(
        string content
    )
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        isSending = true;
        errorMessage = null;
        StateHasChanged();
        try
        {
            // Dispatch command to aggregate via ChatService
            await ChatService.SendMessageAsync(ChannelId, content);

            // Optimistically add the message to the UI
            // In a full implementation, the projection subscription would handle this
            messages.Add(
                new()
                {
                    MessageId = $"msg-{DateTime.UtcNow.Ticks}",
                    Content = content,
                    AuthorUserId = Session.UserId!,
                    AuthorDisplayName = Session.DisplayName ?? "You",
                    SentAt = DateTimeOffset.UtcNow,
                });

            // Scroll to bottom would happen here via JS interop
        }
        catch (ChatOperationException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isSending = false;
            StateHasChanged();
        }
    }

    private async Task LoadChannelDataAsync()
    {
        isLoading = true;
        StateHasChanged();
        try
        {
            // In a full implementation, this would subscribe to a ChannelMessagesProjection
            // For now, we show the pattern with placeholder data
            await Task.Delay(100); // Simulate async loading

            // Clear and reset - actual data would come from projection subscription
            messages.Clear();
            members.Clear();

            // Add the current user as a member (placeholder)
            if (Session.IsAuthenticated)
            {
                members.Add(
                    new()
                    {
                        UserId = Session.UserId!,
                        DisplayName = Session.DisplayName ?? "You",
                        IsOnline = true,
                    });
            }
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}