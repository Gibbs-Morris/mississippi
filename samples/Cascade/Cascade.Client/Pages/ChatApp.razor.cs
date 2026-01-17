using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Cascade.Client.Chat;
using Cascade.Contracts.Api;
using Cascade.Contracts.Projections;

using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Reservoir.Blazor;


namespace Cascade.Client.Pages;

/// <summary>
///     Chat application page implementing real-time messaging with Inlet/Reservoir state management.
/// </summary>
/// <remarks>
///     <para>
///         This component demonstrates the Mississippi collection pattern:
///     </para>
///     <list type="bullet">
///         <item>Subscribe to ID projections (lightweight, always loaded)</item>
///         <item>Subscribe to individual item projections as they enter the viewport</item>
///         <item>Unsubscribe from items as they leave the viewport</item>
///     </list>
///     <para>
///         This enables efficient rendering of large lists without loading all data upfront.
///     </para>
/// </remarks>
public sealed partial class ChatApp
    : StoreComponent,
      IAsyncDisposable
{
    /// <summary>
    ///     The singleton projection ID for all channel IDs.
    /// </summary>
    private const string AllChannelIdsProjectionId = "all";

    private string displayName = string.Empty;

    private bool hasRendered;

    private bool isAllChannelIdsSubscribed;

    private bool isCreatingChannel;

    private string newChannelName = string.Empty;

    private string? previousMessageIdsChannelSubscription;

    private string? previousUserChannelListSubscription;

    /// <summary>
    ///     Tracks which channel summaries are currently subscribed.
    /// </summary>
    private HashSet<string> subscribedChannelSummaries = [];

    /// <summary>
    ///     Tracks which individual messages are currently subscribed.
    /// </summary>
    private HashSet<string> subscribedMessages = [];

    /// <summary>
    ///     Gets available channel IDs from the AllChannelIds projection.
    /// </summary>
    private IReadOnlySet<string>? AvailableChannelIds
    {
        get
        {
            AllChannelIdsDto? projection = InletStore.GetProjection<AllChannelIdsDto>(AllChannelIdsProjectionId);
            return projection?.ChannelIds;
        }
    }

    private ChatState ChatState => GetState<ChatState>();

    /// <summary>
    ///     Gets the current message IDs from the ChannelMessageIds projection.
    /// </summary>
    private IReadOnlyList<string>? CurrentMessageIds
    {
        get
        {
            if (string.IsNullOrEmpty(ChatState.SelectedChannelId))
            {
                return null;
            }

            ChannelMessageIdsDto? projection = InletStore.GetProjection<ChannelMessageIdsDto>(
                ChatState.SelectedChannelId);
            return projection?.MessageIds;
        }
    }

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private IInletStore InletStore { get; set; } = null!;

    private bool IsLoadingMessages =>
        !string.IsNullOrEmpty(ChatState.SelectedChannelId) &&
        InletStore.IsProjectionLoading<ChannelMessageIdsDto>(ChatState.SelectedChannelId);

    private string SelectedChannelName
    {
        get
        {
            if (string.IsNullOrEmpty(ChatState.SelectedChannelId))
            {
                return "Channel";
            }

            ChannelSummaryDto? summary = GetChannelSummary(ChatState.SelectedChannelId);
            return summary?.Name ?? "Channel";
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        UnsubscribeFromAllProjections();
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(
        bool firstRender
    )
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            hasRendered = true;
        }

        if (!hasRendered)
        {
            return;
        }

        // Manage projection subscriptions based on current state
        ManageAllChannelIdsSubscription();
        ManageChannelSummarySubscriptions();
        ManageMessageIdsSubscription();
        ManageMessageSubscriptions();
        ManageUserChannelListSubscription();
    }

    /// <summary>
    ///     Gets available channels by combining IDs with individual summaries.
    /// </summary>
    /// <returns>A list of channel information for display.</returns>
    private List<ChannelInfo> GetAvailableChannels()
    {
        IReadOnlySet<string>? channelIds = AvailableChannelIds;
        if (channelIds is null || (channelIds.Count == 0))
        {
            return [];
        }

        return channelIds.Select(id => GetChannelSummary(id))
            .Where(c => c is not null && !c.IsArchived)
            .OrderBy(c => c!.Name)
            .Select(c => new ChannelInfo
            {
                ChannelId = c!.ChannelId,
                Name = c.Name,
            })
            .ToList();
    }

    private ChannelSummaryDto? GetChannelSummary(
        string channelId
    ) =>
        InletStore.GetProjection<ChannelSummaryDto>(channelId);

    /// <summary>
    ///     Gets the current messages by combining IDs with individual message projections.
    /// </summary>
    /// <returns>A list of messages for display.</returns>
    private List<MessageDto>? GetCurrentMessages()
    {
        IReadOnlyList<string>? messageIds = CurrentMessageIds;
        if (messageIds is null)
        {
            return null;
        }

        return messageIds.Select(id => InletStore.GetProjection<MessageDto>(id))
            .Where(m => m is not null && !m.IsDeleted)
            .ToList()!;
    }

    private void HandleCloseModal() => Dispatch(new HideCreateChannelModalAction());

    private async Task HandleCreateChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(newChannelName) || isCreatingChannel)
        {
            return;
        }

        isCreatingChannel = true;
        try
        {
            string channelId = $"channel-{Guid.NewGuid():N}"[..20];
            string trimmedName = newChannelName.Trim();
            using HttpResponseMessage response = await Http.PostAsync(
                new Uri(
                    $"/api/channels/{Uri.EscapeDataString(channelId)}/create?name={Uri.EscapeDataString(trimmedName)}&createdBy={Uri.EscapeDataString(ChatState.UserId)}",
                    UriKind.Relative),
                null);
            if (response.IsSuccessStatusCode)
            {
                using HttpResponseMessage conversation = await Http.PostAsync(
                    new Uri($"/api/conversations/{Uri.EscapeDataString(channelId)}/start", UriKind.Relative),
                    null);
                conversation.EnsureSuccessStatusCode();
                await JoinChannelAsync(channelId, trimmedName);
                Dispatch(new HideCreateChannelModalAction());
                Dispatch(new SelectChannelAction(channelId));
                newChannelName = string.Empty;
            }
        }
        catch (HttpRequestException)
        {
            // Silently fail for demo
        }
        finally
        {
            isCreatingChannel = false;
        }
    }

    private async Task HandleLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(displayName) || ChatState.IsLoggingIn)
        {
            return;
        }

        Dispatch(new LoginInProgressAction());
        try
        {
            string userId = $"user-{Guid.NewGuid():N}"[..16];
            string trimmedName = displayName.Trim();
            using HttpResponseMessage response = await Http.PostAsync(
                new Uri(
                    $"/api/users/{Uri.EscapeDataString(userId)}/register?displayName={Uri.EscapeDataString(trimmedName)}",
                    UriKind.Relative),
                null);
            if (response.IsSuccessStatusCode)
            {
                Dispatch(new LoginSuccessAction(userId, trimmedName));
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                Dispatch(new LoginFailedAction($"Registration failed: {content}"));
            }
        }
        catch (HttpRequestException ex)
        {
            Dispatch(new LoginFailedAction($"Connection error: {ex.Message}"));
        }
    }

    private void HandleLogout()
    {
        UnsubscribeFromAllProjections();
        Dispatch(new LogoutAction());
    }

    private async Task HandleSelectChannelAsync(
        string channelId
    )
    {
        await JoinChannelAsync(channelId);
        Dispatch(new SelectChannelAction(channelId));
    }

    private async Task HandleSendMessageAsync(
        string message
    )
    {
        if (string.IsNullOrEmpty(ChatState.SelectedChannelId))
        {
            return;
        }

        try
        {
            SendMessageRequest request = new()
            {
                Content = message,
                SentBy = ChatState.UserDisplayName,
            };
            _ = await Http.PostAsJsonAsync(
                new Uri(
                    $"/api/conversations/{Uri.EscapeDataString(ChatState.SelectedChannelId)}/messages",
                    UriKind.Relative),
                request);
        }
        catch (HttpRequestException)
        {
            // Silently fail for demo
        }
    }

    private void HandleShowCreateChannel() => Dispatch(new ShowCreateChannelModalAction());

    private async Task JoinChannelAsync(
        string channelId,
        string? channelName = null
    )
    {
        if (string.IsNullOrEmpty(channelName))
        {
            channelName = GetChannelSummary(channelId)?.Name ?? channelId;
        }

        try
        {
            string nameParam = string.IsNullOrEmpty(channelName)
                ? string.Empty
                : $"&channelName={Uri.EscapeDataString(channelName)}";
            using HttpResponseMessage response = await Http.PostAsync(
                new Uri(
                    $"/api/users/{Uri.EscapeDataString(ChatState.UserId)}/channels/{Uri.EscapeDataString(channelId)}/join?{nameParam}",
                    UriKind.Relative),
                null);
            _ = response;
        }
        catch (HttpRequestException)
        {
            // Silently fail
        }
    }

    /// <summary>
    ///     Manages subscription to AllChannelIds (singleton "all").
    /// </summary>
    private void ManageAllChannelIdsSubscription()
    {
        bool shouldSubscribe = ChatState.IsAuthenticated;
        if (shouldSubscribe && !isAllChannelIdsSubscribed)
        {
            Dispatch(new SubscribeToProjectionAction<AllChannelIdsDto>(AllChannelIdsProjectionId));
            isAllChannelIdsSubscribed = true;
        }
        else if (!shouldSubscribe && isAllChannelIdsSubscribed)
        {
            Dispatch(new UnsubscribeFromProjectionAction<AllChannelIdsDto>(AllChannelIdsProjectionId));
            isAllChannelIdsSubscribed = false;
        }
    }

    /// <summary>
    ///     Manages subscriptions to individual ChannelSummary projections.
    /// </summary>
    /// <remarks>
    ///     Subscribes to all channel summaries when IDs are available.
    ///     In a production app with many channels, you'd only subscribe to visible ones.
    /// </remarks>
    private void ManageChannelSummarySubscriptions()
    {
        IReadOnlySet<string>? currentIds = AvailableChannelIds;
        HashSet<string> targetIds = currentIds?.ToHashSet() ?? [];

        // Subscribe to new channels
        foreach (string id in targetIds.Except(subscribedChannelSummaries))
        {
            Dispatch(new SubscribeToProjectionAction<ChannelSummaryDto>(id));
        }

        // Unsubscribe from removed channels
        foreach (string id in subscribedChannelSummaries.Except(targetIds))
        {
            Dispatch(new UnsubscribeFromProjectionAction<ChannelSummaryDto>(id));
        }

        subscribedChannelSummaries = targetIds;
    }

    /// <summary>
    ///     Manages subscription to ChannelMessageIds for the selected channel.
    /// </summary>
    private void ManageMessageIdsSubscription()
    {
        string? currentChannel = ChatState.SelectedChannelId;
        if (currentChannel != previousMessageIdsChannelSubscription)
        {
            if (!string.IsNullOrEmpty(previousMessageIdsChannelSubscription))
            {
                Dispatch(
                    new UnsubscribeFromProjectionAction<ChannelMessageIdsDto>(previousMessageIdsChannelSubscription));
            }

            if (!string.IsNullOrEmpty(currentChannel))
            {
                Dispatch(new SubscribeToProjectionAction<ChannelMessageIdsDto>(currentChannel));
            }

            previousMessageIdsChannelSubscription = currentChannel;
        }
    }

    /// <summary>
    ///     Manages subscriptions to individual Message projections.
    /// </summary>
    /// <remarks>
    ///     Currently subscribes to all messages in the channel.
    ///     A production implementation would use viewport detection to only
    ///     subscribe to visible messages and implement virtual scrolling.
    /// </remarks>
    private void ManageMessageSubscriptions()
    {
        IReadOnlyList<string>? currentIds = CurrentMessageIds;
        HashSet<string> targetIds = currentIds?.ToHashSet() ?? [];

        // Subscribe to new messages
        foreach (string id in targetIds.Except(subscribedMessages))
        {
            Dispatch(new SubscribeToProjectionAction<MessageDto>(id));
        }

        // Unsubscribe from removed messages (channel changed)
        foreach (string id in subscribedMessages.Except(targetIds))
        {
            Dispatch(new UnsubscribeFromProjectionAction<MessageDto>(id));
        }

        subscribedMessages = targetIds;
    }

    private void ManageUserChannelListSubscription()
    {
        string? expectedSubscription = ChatState.IsAuthenticated ? ChatState.UserId : null;
        if (expectedSubscription != previousUserChannelListSubscription)
        {
            if (!string.IsNullOrEmpty(previousUserChannelListSubscription))
            {
                Dispatch(new UnsubscribeFromProjectionAction<UserChannelListDto>(previousUserChannelListSubscription));
            }

            if (!string.IsNullOrEmpty(expectedSubscription))
            {
                Dispatch(new SubscribeToProjectionAction<UserChannelListDto>(expectedSubscription));
            }

            previousUserChannelListSubscription = expectedSubscription;
        }
    }

    private void UnsubscribeFromAllProjections()
    {
        // Unsubscribe from AllChannelIds
        if (isAllChannelIdsSubscribed)
        {
            Dispatch(new UnsubscribeFromProjectionAction<AllChannelIdsDto>(AllChannelIdsProjectionId));
            isAllChannelIdsSubscribed = false;
        }

        // Unsubscribe from all channel summaries
        foreach (string id in subscribedChannelSummaries)
        {
            Dispatch(new UnsubscribeFromProjectionAction<ChannelSummaryDto>(id));
        }

        subscribedChannelSummaries = [];

        // Unsubscribe from message IDs
        if (!string.IsNullOrEmpty(previousMessageIdsChannelSubscription))
        {
            Dispatch(new UnsubscribeFromProjectionAction<ChannelMessageIdsDto>(previousMessageIdsChannelSubscription));
            previousMessageIdsChannelSubscription = null;
        }

        // Unsubscribe from all individual messages
        foreach (string id in subscribedMessages)
        {
            Dispatch(new UnsubscribeFromProjectionAction<MessageDto>(id));
        }

        subscribedMessages = [];

        // Unsubscribe from user channel list
        if (!string.IsNullOrEmpty(previousUserChannelListSubscription))
        {
            Dispatch(new UnsubscribeFromProjectionAction<UserChannelListDto>(previousUserChannelListSubscription));
            previousUserChannelListSubscription = null;
        }
    }
}