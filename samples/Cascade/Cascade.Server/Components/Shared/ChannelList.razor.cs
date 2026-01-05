// <copyright file="ChannelList.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Server.Services;

using Microsoft.AspNetCore.Components;

using Mississippi.Ripples.Abstractions;


namespace Cascade.Server.Components.Shared;

/// <summary>
///     Component for displaying a list of channels the user belongs to.
/// </summary>
public sealed partial class ChannelList
    : ComponentBase,
      IAsyncDisposable
{
    private bool showCreateModal;

    private IProjectionSubscription<UserProfileProjection>? subscription;

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
    private IProjectionCache ProjectionCache { get; set; } = default!;

    [Inject]
    private UserSession UserSession { get; set; } = default!;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (subscription is not null)
        {
            await subscription.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (UserSession.IsAuthenticated)
        {
            subscription = await ProjectionCache.SubscribeAsync<UserProfileProjection>(
                UserSession.UserId!,
                () => _ = InvokeAsync(StateHasChanged));
        }
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