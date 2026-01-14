using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.Message;

/// <summary>
///     Read-optimized projection for an individual message.
/// </summary>
/// <remarks>
///     <para>
///         This projection represents a single message, enabling granular subscriptions.
///         Clients subscribe to individual messages as they scroll into view,
///         rather than loading entire message histories.
///     </para>
///     <para>
///         Subscribes to events from the Conversation aggregate:
///         MessageSent, MessageEdited, MessageDeleted.
///     </para>
///     <para>
///         The projection is keyed by the message ID, which includes the channel prefix
///         to ensure uniqueness (e.g., "channel-abc/msg-123").
///     </para>
/// </remarks>
[ProjectionPath("cascade/messages")]
[UxProjection]
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
[SnapshotStorageName("CASCADE", "CHAT", "MESSAGE")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.Message.MessageProjection")]
public sealed record MessageProjection
{
    /// <summary>
    ///     Gets the channel or conversation this message belongs to.
    /// </summary>
    [Id(5)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    [Id(1)]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp when the message was edited, if applicable.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    [Id(6)]
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public string MessageId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    [Id(3)]
    public DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    [Id(2)]
    public string SentBy { get; init; } = string.Empty;
}