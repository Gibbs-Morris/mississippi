using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.User.Commands;

/// <summary>
///     Command to record that a user has left a channel.
/// </summary>
[GenerateCommand(Route = "leave-channel")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.LeaveChannel")]
internal sealed record LeaveChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }
}