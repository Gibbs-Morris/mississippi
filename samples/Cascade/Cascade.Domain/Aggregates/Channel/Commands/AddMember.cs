using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Channel.Commands;

/// <summary>
///     Command to add a member to a channel.
/// </summary>
[GenerateCommand(Route = "add-member")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.AddMember")]
internal sealed record AddMember
{
    /// <summary>
    ///     Gets the user ID of the member being added.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}