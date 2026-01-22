using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Aggregates.User.Events;

/// <summary>
///     Event raised when a user comes online.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "USERWENTONLINE")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserWentOnline")]
internal sealed record UserWentOnline
{
    /// <summary>
    ///     Gets the timestamp when the user came online.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset Timestamp { get; init; }
}