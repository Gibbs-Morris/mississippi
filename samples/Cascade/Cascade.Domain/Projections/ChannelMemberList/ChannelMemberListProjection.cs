using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Projections.ChannelMemberList;

/// <summary>
///     Read-optimized projection of channel members for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of channel membership
///         optimized for display in UI components like the member sidebar.
///     </para>
///     <para>
///         Subscribes to events from the Channel aggregate:
///         ChannelCreated, MemberAdded, MemberRemoved.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "CHANNEL")]
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMEMBERLIST", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelMemberList.ChannelMemberListProjection")]
internal sealed record ChannelMemberListProjection
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(1)]
    public string ChannelName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the total count of members.
    /// </summary>
    [Id(3)]
    public int MemberCount { get; init; }

    /// <summary>
    ///     Gets the list of members in the channel.
    /// </summary>
    [Id(2)]
    public ImmutableList<MemberInfo> Members { get; init; } = ImmutableList<MemberInfo>.Empty;
}