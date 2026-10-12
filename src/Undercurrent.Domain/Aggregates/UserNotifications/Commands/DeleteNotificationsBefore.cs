using System;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests deletion of notifications created strictly before the supplied instant.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands.DeleteNotificationsBefore")]
internal sealed record DeleteNotificationsBefore
{
    /// <summary>
    ///     Gets the exclusive creation-time cutoff; notifications exactly at this instant remain.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset Before { get; init; }
}