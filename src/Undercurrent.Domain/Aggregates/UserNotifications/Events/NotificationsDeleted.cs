using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

/// <summary>
///     Records the exact notifications selected for deletion by age.
/// </summary>
[EventStorageName("MISSISSIPPI", "UNDERCURRENT", "NOTIFICATIONSDELETED", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events.NotificationsDeleted")]
internal sealed record NotificationsDeleted
{
    /// <summary>
    ///     Gets the selected notification identifiers in ordinal order.
    /// </summary>
    [Id(0)]
    public required ImmutableArray<string> NotificationIds { get; init; }

    /// <summary>
    ///     Gets the exclusive UTC creation-time cutoff used for selection.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset OlderThan { get; init; }
}