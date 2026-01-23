using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Channel.Commands;

/// <summary>
///     Command to create a new channel.
/// </summary>
[GenerateCommand(Route = "create")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.CreateChannel")]
internal sealed record CreateChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    [Id(2)]
    public required string CreatedBy { get; init; }

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(1)]
    public required string Name { get; init; }
}