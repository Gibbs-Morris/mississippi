using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Channel.Commands;

/// <summary>
///     Command to rename a channel.
/// </summary>
[GenerateCommand(Route = "rename")]
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