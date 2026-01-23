using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Aggregates.User.Events;

/// <summary>
///     Event raised when a user goes offline.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "USERWENTOFFLINE")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserWentOffline")]
internal sealed record UserWentOffline
{
    /// <summary>
    ///     Gets the timestamp when the user went offline.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset Timestamp { get; init; }
}