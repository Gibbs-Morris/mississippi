using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.User.Commands;

/// <summary>
///     Command to record that a user has joined a channel.
/// </summary>
[GenerateCommand(Route = "join-channel")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.JoinChannel")]
internal sealed record JoinChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the channel name (denormalized for display in projections).
    /// </summary>
    [Id(1)]
    public required string ChannelName { get; init; }
}