using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.AllChannelIds;

/// <summary>
///     Read-optimized projection containing only channel IDs.
/// </summary>
/// <remarks>
///     <para>
///         This lightweight singleton projection enables efficient channel discovery.
///         Instead of loading all channel details, clients subscribe to this ID set,
///         then subscribe to individual <see cref="ChannelSummary.ChannelSummaryProjection" /> items.
///     </para>
///     <para>
///         Subscribes to events from the Channel aggregate:
///         ChannelCreated, ChannelArchived.
///     </para>
///     <para>
///         This is a singleton projection keyed by "all".
///     </para>
/// </remarks>
[ProjectionPath("cascade/channel-ids")]
[UxProjection]
[BrookName("CASCADE", "CHAT", "CHANNEL")]
[SnapshotStorageName("CASCADE", "CHAT", "ALLCHANNELIDS")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.AllChannelIds.AllChannelIdsProjection")]
public sealed record AllChannelIdsProjection
{
    /// <summary>
    ///     Gets the set of all active (non-archived) channel IDs.
    /// </summary>
    [Id(0)]
    public ImmutableHashSet<string> ChannelIds { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    ///     Gets the total count of channels.
    /// </summary>
    [Id(1)]
    public int TotalCount { get; init; }
}