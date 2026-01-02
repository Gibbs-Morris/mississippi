using Orleans;


namespace Cascade.Domain.Channel.Commands;

/// <summary>
///     Command to archive a channel.
/// </summary>
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