using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.User.Events;

/// <summary>
///     Event raised when a user is registered.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "USERREGISTERED", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserRegistered")]
internal sealed record UserRegistered
{
    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user registered.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    ///     Gets the user's unique identifier.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}