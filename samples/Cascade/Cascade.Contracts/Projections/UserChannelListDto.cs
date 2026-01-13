using System.Collections.Immutable;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     DTO for the user's channel list projection, consumed by the Blazor client.
/// </summary>
/// <remarks>
///     This mirrors the internal <c>UserChannelListProjection</c> from the domain
///     but is publicly accessible for client consumption via Inlet subscriptions.
/// </remarks>
[ProjectionPath("cascade/users")]
public sealed record UserChannelListDto
{
    /// <summary>
    ///     Gets the list of channels the user belongs to.
    /// </summary>
    public ImmutableList<ChannelMembershipDto> Channels { get; init; } = [];

    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}