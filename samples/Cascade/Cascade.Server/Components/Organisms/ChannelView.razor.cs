using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain.Projections.ChannelMemberList;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Server.Services;
using Cascade.Server.ViewModels;

using Microsoft.AspNetCore.Components;

using Mississippi.Ripples.Abstractions.State;
using Mississippi.Ripples.Blazor;


namespace Cascade.Server.Components.Organisms;

/// <summary>
///     Organism component that owns state and dispatches commands.
/// </summary>
public sealed partial class ChannelView : RippleComponent
{
    private readonly List<MemberViewModel> members = [];

    private readonly List<MessageViewModel> messages = [];

    private string? errorMessage;

    private bool isLoading = true;

    private bool isSending;

    private string? lastChannelId;

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

    /// <summary>
    ///     Gets the channel member list projection state.
    /// </summary>
    private IProjectionState<ChannelMemberListProjection>? MembersState =>
        !string.IsNullOrEmpty(ChannelId) ? GetProjectionState<ChannelMemberListProjection>(ChannelId) : null;

    /// <summary>
    ///     Gets the channel messages projection state.
    /// </summary>
    private IProjectionState<ChannelMessagesProjection>? MessagesState =>
        !string.IsNullOrEmpty(ChannelId) ? GetProjectionState<ChannelMessagesProjection>(ChannelId) : null;

    [Inject]
    private UserSession Session { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrEmpty(ChannelId))
        {
            return;
        }

        // Unsubscribe from old channel if changed
        if ((lastChannelId != null) && (lastChannelId != ChannelId))
        {
            UnsubscribeFromProjection<ChannelMessagesProjection>(lastChannelId);
            UnsubscribeFromProjection<ChannelMemberListProjection>(lastChannelId);
        }

        // Subscribe to new channel
        if (lastChannelId != ChannelId)
        {
            isLoading = true;
            SubscribeToProjection<ChannelMessagesProjection>(ChannelId);
            SubscribeToProjection<ChannelMemberListProjection>(ChannelId);
            lastChannelId = ChannelId;
        }

        // Update view models from projection state
        UpdateMessages();
        UpdateMembers();

        // Check loading state
        if ((MessagesState != null) && !MessagesState.IsLoading)
        {
            isLoading = false;
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

            // The projection subscription will automatically update the UI when
            // the aggregate emits the MessageSent event and the projection is refreshed.
            // No optimistic UI update needed - real-time SignalR will handle it.
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

    private void UpdateMembers()
    {
        if (MembersState?.Data is { } projection)
        {
            members.Clear();
            members.AddRange(
                projection.Members.Select(member => new MemberViewModel
                {
                    UserId = member.UserId,
                    DisplayName = member.UserId, // Could be enhanced with user lookup
                    IsOnline = true, // Could be tracked via separate projection
                }));
        }
    }

    private void UpdateMessages()
    {
        if (MessagesState?.Data is { } projection)
        {
            messages.Clear();
            messages.AddRange(
                projection.Messages.Select(message => new MessageViewModel
                {
                    MessageId = message.MessageId,
                    Content = message.Content,
                    AuthorUserId = message.SentBy,
                    AuthorDisplayName = message.SentBy, // Could be enhanced with user lookup
                    SentAt = message.SentAt,
                }));
        }
    }
}