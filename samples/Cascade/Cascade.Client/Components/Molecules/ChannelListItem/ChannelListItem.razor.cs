using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Cascade.Client.Components.Molecules.ChannelListItem;

/// <summary>
///     Molecule component for displaying a channel item in the sidebar navigation.
/// </summary>
public sealed partial class ChannelListItem : ComponentBase
{
    /// <summary>
    ///     Gets or sets the channel identifier.
    /// </summary>
    [Parameter]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this channel is currently active/selected.
    /// </summary>
    [Parameter]
    public bool IsActive { get; set; }

    /// <summary>
    ///     Gets or sets the channel display name.
    /// </summary>
    [Parameter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the callback invoked when the channel is selected.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSelect { get; set; }

    /// <summary>
    ///     Gets or sets the unread message count for this channel.
    /// </summary>
    [Parameter]
    public int UnreadCount { get; set; }

    private Task HandleClickAsync() => OnSelect.InvokeAsync(ChannelId);

    private Task HandleKeyPressAsync(
        KeyboardEventArgs e
    ) =>
        (e.Key == "Enter") || (e.Key == " ") ? OnSelect.InvokeAsync(ChannelId) : Task.CompletedTask;
}