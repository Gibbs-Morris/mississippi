// <copyright file="ChannelList.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Server.Components.Services;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Shared;

/// <summary>
///     Component for displaying a list of channels the user belongs to.
/// </summary>
public sealed partial class ChannelList
    : ComponentBase,
      IAsyncDisposable
{
    private bool showCreateModal;

    private IProjectionSubscriber<UserProfileProjection>? subscriber;

    /// <summary>
    ///     Gets or sets the callback when a channel is selected.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnChannelSelected { get; set; }

    /// <summary>
    ///     Gets or sets the currently selected channel ID.
    /// </summary>
    [Parameter]
    public string? SelectedChannelId { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IProjectionSubscriberFactory SubscriberFactory { get; set; } = default!;

    [Inject]
    private UserSession UserSession { get; set; } = default!;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (subscriber is not null)
        {
            subscriber.OnChanged -= HandleProjectionChanged;
            await subscriber.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (UserSession.IsAuthenticated)
        {
            subscriber = SubscriberFactory.Create<UserProfileProjection>();
            subscriber.OnChanged += HandleProjectionChanged;
            await subscriber.SubscribeAsync(UserSession.UserId!);
        }
    }

    private void HandleProjectionChanged(
        object? sender,
        EventArgs e
    )
    {
        // Fire-and-forget is intentional for UI state updates from events.
        // InvokeAsync marshals StateHasChanged to the correct synchronization context.
#pragma warning disable VSTHRD110 // Observe the awaitable result of this method call
        _ = InvokeAsync(StateHasChanged);
#pragma warning restore VSTHRD110
    }

    private void HideCreateModal() => showCreateModal = false;

    private async Task OnChannelCreatedAsync(
        string channelId
    )
    {
        showCreateModal = false;
        await SelectChannelAsync(channelId);
    }

    private async Task SelectChannelAsync(
        string channelId
    )
    {
        await OnChannelSelected.InvokeAsync(channelId);
    }

    private void ShowCreateModal() => showCreateModal = true;
}