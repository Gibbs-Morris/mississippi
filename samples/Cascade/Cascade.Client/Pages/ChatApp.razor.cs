using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
public sealed partial class ChatApp
    : StoreComponent,
      IAsyncDisposable
{
    private string displayName = string.Empty;

    private bool hasRendered;

    private bool isCreatingChannel;

    private bool isLoggingIn;

    private string? loginError;

    private string newChannelName = string.Empty;

    private string? previousChannelSubscription;

    private string? previousUserChannelListSubscription;

    private ChatState ChatState => GetState<ChatState>();

    private IReadOnlyList<ChannelMessageItem>? CurrentMessages
    {
        get
        {
            if (string.IsNullOrEmpty(ChatState.SelectedChannelId))
            {
                return null;
            }

            ChannelMessagesDto? projection = InletStore.GetProjection<ChannelMessagesDto>(ChatState.SelectedChannelId);
            return projection?.Messages;
        }
    }

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private IInletStore InletStore { get; set; } = null!;

    private bool IsLoadingMessages =>
        !string.IsNullOrEmpty(ChatState.SelectedChannelId) &&
        InletStore.IsProjectionLoading<ChannelMessagesDto>(ChatState.SelectedChannelId);

    private string SelectedChannelName =>
        ChatState.Channels.FirstOrDefault(c => c.ChannelId == ChatState.SelectedChannelId)?.Name ?? "Channel";

    private static bool ChannelsMatch(
        ImmutableList<ChannelInfo> current,
        List<ChannelInfo> incoming
    )
    {
        if (current.Count != incoming.Count)
        {
            return false;
        }

        for (int i = 0; i < current.Count; i++)
        {
            if (current[i].ChannelId != incoming[i].ChannelId)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from all projections when leaving the page
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

        // Only manage subscriptions after first render
        if (!hasRendered)
        {
            return;
        }

        // Handle user channel list subscription based on authentication
        await ManageUserChannelListSubscriptionAsync();

        // Handle message subscription based on selected channel
        ManageMessageSubscription();
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
            // Generate a unique channel ID
            string channelId = $"channel-{Guid.NewGuid():N}"[..20];
            string trimmedName = newChannelName.Trim();

            // Create the channel via API
            using HttpResponseMessage response = await Http.PostAsync(
                new Uri(
                    $"/api/channels/{Uri.EscapeDataString(channelId)}/create?name={Uri.EscapeDataString(trimmedName)}&createdBy={Uri.EscapeDataString(ChatState.UserId)}",
                    UriKind.Relative),
                null);
            if (response.IsSuccessStatusCode)
            {
                // Start a conversation for the channel
                using HttpResponseMessage conversation = await Http.PostAsync(
                    new Uri($"/api/conversations/{Uri.EscapeDataString(channelId)}/start", UriKind.Relative),
                    null);
                conversation.EnsureSuccessStatusCode();

                // Join the channel (updates both user and channel aggregates)
                await JoinChannelAsync(channelId, trimmedName);

                // Add channel locally and select it
                ChannelInfo newChannel = new()
                {
                    ChannelId = channelId,
                    Name = trimmedName,
                };
                Dispatch(new ChannelCreatedAction(newChannel));
                Dispatch(new HideCreateChannelModalAction());
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
        if (string.IsNullOrWhiteSpace(displayName) || isLoggingIn)
        {
            return;
        }

        isLoggingIn = true;
        loginError = null;
        Dispatch(new LoginInProgressAction());
        try
        {
            // Generate a unique user ID for this session
            string userId = $"user-{Guid.NewGuid():N}"[..16];
            string trimmedName = displayName.Trim();

            // Register the user via API
            using HttpResponseMessage response = await Http.PostAsync(
                new Uri(
                    $"/api/users/{Uri.EscapeDataString(userId)}/register?displayName={Uri.EscapeDataString(trimmedName)}",
                    UriKind.Relative),
                null);
            if (response.IsSuccessStatusCode)
            {
                Dispatch(new LoginSuccessAction(userId, trimmedName));

                // Subscription to user's channel list happens in OnAfterRenderAsync
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                loginError = $"Registration failed: {content}";
                Dispatch(new LoginFailedAction(loginError));
            }
        }
        catch (HttpRequestException ex)
        {
            loginError = $"Connection error: {ex.Message}";
            Dispatch(new LoginFailedAction(loginError));
        }
        finally
        {
            isLoggingIn = false;
        }
    }

    private void HandleLogout()
    {
        // Clean up all subscriptions before logout
        UnsubscribeFromAllProjections();
        Dispatch(new LogoutAction());
    }

    private async Task HandleSelectChannelAsync(
        string channelId
    )
    {
        // Join the channel if not already joined
        await JoinChannelAsync(channelId);
        Dispatch(new SelectChannelAction(channelId));

        // Subscription happens in OnAfterRenderAsync when state updates
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
            // Silently fail for demo; production would show error toast
        }
    }

    private void HandleShowCreateChannel() => Dispatch(new ShowCreateChannelModalAction());

    private async Task JoinChannelAsync(
        string channelId,
        string? channelName = null
    )
    {
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

            // Ignore errors - join is idempotent
            _ = response;
        }
        catch (HttpRequestException)
        {
            // Silently fail - join is best-effort
        }
    }

    private void ManageMessageSubscription()
    {
        if (ChatState.SelectedChannelId != previousChannelSubscription)
        {
            // Unsubscribe from previous
            if (!string.IsNullOrEmpty(previousChannelSubscription))
            {
                Dispatch(new UnsubscribeFromProjectionAction<ChannelMessagesDto>(previousChannelSubscription));
            }

            // Subscribe to new
            if (!string.IsNullOrEmpty(ChatState.SelectedChannelId))
            {
                Dispatch(new SubscribeToProjectionAction<ChannelMessagesDto>(ChatState.SelectedChannelId));
            }

            previousChannelSubscription = ChatState.SelectedChannelId;
        }
    }

    private async Task ManageUserChannelListSubscriptionAsync()
    {
        string? expectedSubscription = ChatState.IsAuthenticated ? ChatState.UserId : null;
        if (expectedSubscription != previousUserChannelListSubscription)
        {
            // Unsubscribe from previous
            if (!string.IsNullOrEmpty(previousUserChannelListSubscription))
            {
                Dispatch(new UnsubscribeFromProjectionAction<UserChannelListDto>(previousUserChannelListSubscription));
            }

            // Subscribe to new user's channel list
            if (!string.IsNullOrEmpty(expectedSubscription))
            {
                Dispatch(new SubscribeToProjectionAction<UserChannelListDto>(expectedSubscription));

                // Sync channels from projection when it loads
                await SyncChannelsFromProjectionAsync();
            }

            previousUserChannelListSubscription = expectedSubscription;
        }
        else if (ChatState.IsAuthenticated && !string.IsNullOrEmpty(ChatState.UserId))
        {
            // Check if we have projection data to sync
            await SyncChannelsFromProjectionAsync();
        }
    }

    private async Task SyncChannelsFromProjectionAsync()
    {
        UserChannelListDto? userChannels = InletStore.GetProjection<UserChannelListDto>(ChatState.UserId);
        if (userChannels is not null)
        {
            // Convert projection to channel info list
            List<ChannelInfo> channels = userChannels.Channels.Select(c => new ChannelInfo
                {
                    ChannelId = c.ChannelId,
                    Name = string.IsNullOrEmpty(c.ChannelName) ? c.ChannelId : c.ChannelName,
                })
                .ToList();

            // Only dispatch if channels changed
            if (!ChannelsMatch(ChatState.Channels, channels))
            {
                Dispatch(new ChannelsLoadedAction(channels));
            }
        }

        await Task.CompletedTask;
    }

    private void UnsubscribeFromAllProjections()
    {
        if (!string.IsNullOrEmpty(previousChannelSubscription))
        {
            Dispatch(new UnsubscribeFromProjectionAction<ChannelMessagesDto>(previousChannelSubscription));
            previousChannelSubscription = null;
        }

        if (!string.IsNullOrEmpty(previousUserChannelListSubscription))
        {
            Dispatch(new UnsubscribeFromProjectionAction<UserChannelListDto>(previousUserChannelListSubscription));
            previousUserChannelListSubscription = null;
        }
    }
}