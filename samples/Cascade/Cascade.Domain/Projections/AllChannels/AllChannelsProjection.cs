using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Inlet.Projection.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.AllChannels;

/// <summary>
///     Read-optimized projection of all available channels for discovery.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of all channels
///         optimized for display in channel browse/discovery UI.
///     </para>
///     <para>
///         Subscribes to events from the Channel aggregate:
///         ChannelCreated, ChannelRenamed, ChannelArchived, MemberAdded, MemberRemoved.
///     </para>
///     <para>
///         This is a singleton projection keyed by "all" that aggregates
///         channel information across all channel aggregates.
///     </para>
/// </remarks>
[ProjectionPath("cascade/discovery")]
[BrookName("CASCADE", "CHAT", "CHANNEL")]
[SnapshotStorageName("CASCADE", "CHAT", "ALLCHANNELS")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.AllChannels.AllChannelsProjection")]
public sealed record AllChannelsProjection
{
    /// <summary>
    ///     Gets the list of all available channels.
    /// </summary>
    [Id(0)]
    public ImmutableDictionary<string, EmbeddedChannelSummary> Channels { get; init; } =
        ImmutableDictionary<string, EmbeddedChannelSummary>.Empty;
}