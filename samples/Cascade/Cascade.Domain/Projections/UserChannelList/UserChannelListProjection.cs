using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Projections.UserChannelList;

/// <summary>
///     Read-optimized projection of channels a user belongs to for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of user's channel memberships
///         optimized for display in UI components like the channel sidebar.
///     </para>
///     <para>
///         Subscribes to events from the User aggregate:
///         UserRegistered, UserJoinedChannel, UserLeftChannel.
///     </para>
/// </remarks>
[ProjectionPath("cascade/users")]
[BrookName("CASCADE", "CHAT", "USER")]
[SnapshotStorageName("CASCADE", "CHAT", "USERCHANNELLIST")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.UserChannelList.UserChannelListProjection")]
internal sealed record UserChannelListProjection
{
    /// <summary>
    ///     Gets the list of channels the user belongs to.
    /// </summary>
    [Id(1)]
    public ImmutableList<ChannelMembership> Channels { get; init; } = ImmutableList<ChannelMembership>.Empty;

    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    [Id(0)]
    public string UserId { get; init; } = string.Empty;
}