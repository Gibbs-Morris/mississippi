using Orleans;


namespace Cascade.Domain.Channel.Commands;

/// <summary>
///     Command to rename a channel.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.RenameChannel")]
internal sealed record RenameChannel
{
    /// <summary>
    ///     Gets the new channel name.
    /// </summary>
    [Id(0)]
    public required string NewName { get; init; }
}