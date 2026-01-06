using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain.Projections.ChannelMemberList;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Server.Services;
using Cascade.Server.ViewModels;

using Microsoft.AspNetCore.Components;

using Mississippi.Ripples.Abstractions;


namespace Cascade.Server.Components.Organisms;

/// <summary>
///     Organism component that owns state and dispatches commands.
/// </summary>
public sealed partial class ChannelView
    : ComponentBase,
      IAsyncDisposable
{
    private readonly List<MemberViewModel> members = [];

    private readonly List<MessageViewModel> messages = [];

    private string? errorMessage;

    private bool isLoading = true;

    private bool isSending;

    private IProjectionBinder<ChannelMemberListProjection>? memberBinder;

    private IProjectionBinder<ChannelMessagesProjection>? messagesBinder;

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
    private IProjectionCache ProjectionCache { get; set; } = default!;

    [Inject]
    private UserSession Session { get; set; } = default!;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (messagesBinder is not null)
        {
            await messagesBinder.DisposeAsync();
        }

        if (memberBinder is not null)
        {
            await memberBinder.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        // Create binders once - they handle entity ID switching automatically
        messagesBinder = ProjectionCache.CreateBinder<ChannelMessagesProjection>();
        memberBinder = ProjectionCache.CreateBinder<ChannelMemberListProjection>();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(ChannelId))
        {
            return;
        }

        isLoading = true;
        StateHasChanged();
        try
        {
            // Bind to projections - binders handle switching if ChannelId changes
            await messagesBinder!.BindAsync(ChannelId, UpdateMessages);
            await memberBinder!.BindAsync(ChannelId, UpdateMembers);

            // Initialize from current values if already loaded
            UpdateMessages();
            UpdateMembers();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
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
        if (memberBinder?.Current is { } projection)
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

        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateMessages()
    {
        if (messagesBinder?.Current is { } projection)
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

        _ = InvokeAsync(StateHasChanged);
    }
}