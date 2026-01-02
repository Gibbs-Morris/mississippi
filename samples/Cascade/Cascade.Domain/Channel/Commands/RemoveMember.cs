using Orleans;


namespace Cascade.Domain.Channel.Commands;

/// <summary>
///     Command to remove a member from a channel.
/// </summary>
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