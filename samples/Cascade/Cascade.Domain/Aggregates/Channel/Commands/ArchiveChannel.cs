using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Channel.Commands;

/// <summary>
///     Command to archive a channel.
/// </summary>
[GenerateCommand(Route = "archive")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.ArchiveChannel")]
internal sealed record ArchiveChannel
{
    /// <summary>
    ///     Gets the user ID of the person archiving the channel.
    /// </summary>
    [Id(0)]
    public required string ArchivedBy { get; init; }
}