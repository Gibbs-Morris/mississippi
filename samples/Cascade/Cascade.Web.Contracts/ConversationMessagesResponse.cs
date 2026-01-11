using System;
using System.Collections.Generic;


namespace Cascade.Web.Contracts;

// TEMPORARY PLUMBING - TO BE REPLACED BY INLET
// This DTO maps the internal ChannelMessagesProjection to a public API contract.
// Once Inlet is integrated, projections will be pushed directly to clients via
// SignalR and this mapping layer may be simplified or removed.

/// <summary>
///     Response containing messages for a conversation.
/// </summary>
public sealed record ConversationMessagesResponse
{
    /// <summary>
    ///     Gets the conversation identifier.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    ///     Gets the total count of messages.
    /// </summary>
    public required int MessageCount { get; init; }

    /// <summary>
    ///     Gets the list of messages.
    /// </summary>
    public required IReadOnlyList<ConversationMessageItem> Messages { get; init; }
}

/// <summary>
///     Represents a message in a conversation.
/// </summary>
public sealed record ConversationMessageItem
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was edited, if applicable.
    /// </summary>
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    public required string SentBy { get; init; }
}