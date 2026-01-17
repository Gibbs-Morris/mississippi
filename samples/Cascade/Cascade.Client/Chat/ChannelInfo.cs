namespace Cascade.Client.Chat;

/// <summary>
///     Represents information about a channel for display in the sidebar.
/// </summary>
public sealed record ChannelInfo
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the channel display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the number of unread messages.
    /// </summary>
    public int UnreadCount { get; init; }
}