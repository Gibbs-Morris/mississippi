using System.Threading.Tasks;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Server.Services;

using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Abstractions.State;
using Mississippi.Inlet.Blazor.WebAssembly;


namespace Cascade.Server.Components.Shared;

/// <summary>
///     Component for displaying a list of channels the user belongs to.
/// </summary>
public sealed partial class ChannelList : InletComponent
{
    private bool showCreateModal;

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

    /// <summary>
    ///     Gets the user profile projection state.
    /// </summary>
    private IProjectionState<UserProfileProjection>? UserProfileState =>
        UserSession.UserId is not null ? GetProjectionState<UserProfileProjection>(UserSession.UserId) : null;

    [Inject]
    private UserSession UserSession { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (UserSession.IsAuthenticated && UserSession.UserId is not null)
        {
            SubscribeToProjection<UserProfileProjection>(UserSession.UserId);
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