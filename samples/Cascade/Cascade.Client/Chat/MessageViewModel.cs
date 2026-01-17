using System;


namespace Cascade.Client.Chat;

/// <summary>
///     Represents a chat message for display.
/// </summary>
public sealed record MessageViewModel
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this message is from the current user.
    /// </summary>
    public bool IsFromCurrentUser { get; init; }

    /// <summary>
    ///     Gets the unique message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the sender's user ID.
    /// </summary>
    public required string SenderId { get; init; }

    /// <summary>
    ///     Gets the sender's display name.
    /// </summary>
    public required string SenderName { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}