using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.ChannelSummary;

/// <summary>
///     Read-optimized projection for an individual channel's summary.
/// </summary>
/// <remarks>
///     <para>
///         This projection represents a single channel's metadata, enabling granular subscriptions.
///         Clients subscribe to individual channel summaries based on the IDs from
///         <see cref="AllChannelIds.AllChannelIdsProjection" />.
///     </para>
///     <para>
///         Subscribes to events from the Channel aggregate:
///         ChannelCreated, ChannelRenamed, ChannelArchived, MemberAdded, MemberRemoved.
///     </para>
/// </remarks>
[ProjectionPath("cascade/channel-summaries")]
[UxProjection]
[BrookName("CASCADE", "CHAT", "CHANNEL")]
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELSUMMARY")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelSummary.ChannelSummaryProjection")]
public sealed record ChannelSummaryProjection
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp when the channel was created.
    /// </summary>
    [Id(2)]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    [Id(3)]
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the channel is archived.
    /// </summary>
    [Id(4)]
    public bool IsArchived { get; init; }

    /// <summary>
    ///     Gets the number of members in the channel.
    /// </summary>
    [Id(5)]
    public int MemberCount { get; init; }

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(1)]
    public string Name { get; init; } = string.Empty;
}