using Orleans;


namespace Cascade.Domain.User.Commands;

/// <summary>
///     Command to set a user's online/offline status.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.SetOnlineStatus")]
internal sealed record SetOnlineStatus
{
    /// <summary>
    ///     Gets a value indicating whether the user is online.
    /// </summary>
    [Id(0)]
    public required bool IsOnline { get; init; }
}