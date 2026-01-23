using System.Collections.Immutable;

using Cascade.Domain.Projections.Message;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.ChannelMessageIds;

/// <summary>
///     Read-optimized projection containing only message IDs for a channel.
/// </summary>
/// <remarks>
///     <para>
///         This lightweight projection enables efficient viewport-based subscriptions.
///         Instead of loading all message content, clients subscribe to this ID list,
///         then subscribe to individual <see cref="MessageProjection" /> items as they scroll.
///     </para>
///     <para>
///         Subscribes to events from the Conversation aggregate:
///         ConversationStarted, MessageSent, MessageDeleted.
///     </para>
/// </remarks>
[ProjectionPath("cascade/channel-message-ids")]
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGEIDS")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelMessageIds.ChannelMessageIdsProjection")]
public sealed record ChannelMessageIdsProjection
{
    /// <summary>
    ///     Gets the channel or conversation identifier.
    /// </summary>
    [Id(0)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the ordered list of message IDs (newest last).
    /// </summary>
    [Id(1)]
    public ImmutableList<string> MessageIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    ///     Gets the total count of messages (including deleted).
    /// </summary>
    [Id(2)]
    public int TotalCount { get; init; }
}