using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Channel.Commands;

/// <summary>
///     Command to remove a member from a channel.
/// </summary>
[GenerateCommand(Route = "remove-member")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.RemoveMember")]
internal sealed record RemoveMember
{
    /// <summary>
    ///     Gets the user ID of the member being removed.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}