using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.User.Events;

/// <summary>
///     Event raised when a user's display name is updated.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "DISPLAYNAMEUPDATED")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.DisplayNameUpdated")]
internal sealed record DisplayNameUpdated
{
    /// <summary>
    ///     Gets the new display name.
    /// </summary>
    [Id(1)]
    public required string NewDisplayName { get; init; }

    /// <summary>
    ///     Gets the old display name.
    /// </summary>
    [Id(0)]
    public required string OldDisplayName { get; init; }
}