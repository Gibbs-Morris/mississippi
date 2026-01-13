using System.Collections.Generic;
using System.Threading.Tasks;

using Cascade.Client.Chat;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Organisms.ChannelSidebar;

/// <summary>
///     Organism component for the channel navigation sidebar with user info.
/// </summary>
public sealed partial class ChannelSidebar : ComponentBase
{
    /// <summary>
    ///     Gets or sets the list of available channels.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ChannelInfo>? Channels { get; set; }

    /// <summary>
    ///     Gets or sets the current user's display name.
    /// </summary>
    [Parameter]
    public string CurrentUserName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the callback invoked when the user wants to create a channel.
    /// </summary>
    [Parameter]
    public EventCallback OnCreateChannel { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the user wants to log out.
    /// </summary>
    [Parameter]
    public EventCallback OnLogout { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when a channel is selected.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSelectChannel { get; set; }

    /// <summary>
    ///     Gets or sets the currently selected channel ID.
    /// </summary>
    [Parameter]
    public string? SelectedChannelId { get; set; }

    private Task HandleChannelSelectAsync(
        string channelId
    ) =>
        OnSelectChannel.InvokeAsync(channelId);

    private Task HandleCreateChannelAsync() => OnCreateChannel.InvokeAsync();

    private Task HandleLogoutAsync() => OnLogout.InvokeAsync();
}