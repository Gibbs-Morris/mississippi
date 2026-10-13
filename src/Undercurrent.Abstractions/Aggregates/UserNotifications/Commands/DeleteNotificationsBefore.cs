using System;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests deletion of notifications created strictly before the supplied instant.
/// </summary>
/// <remarks>Public requests let hosts select an explicit timestamp cutoff for inbox cleanup.</remarks>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands.DeleteNotificationsBefore")]
public sealed record DeleteNotificationsBefore
{
    /// <summary>
    ///     Gets the exclusive creation-time cutoff; notifications exactly at this instant remain.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset Before { get; init; }
}