using Orleans;


namespace Cascade.Domain.User.Commands;

/// <summary>
///     Command to record that a user has joined a channel.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.JoinChannel")]
internal sealed record JoinChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }
}