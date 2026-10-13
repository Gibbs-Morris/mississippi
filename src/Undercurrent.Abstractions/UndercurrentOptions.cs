using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;


namespace Mississippi.Undercurrent.Abstractions;

/// <summary>
///     Provides host configuration for Undercurrent business features.
/// </summary>
/// <remarks>Public so applications can configure the enterprise layer without implementation dependencies.</remarks>
public sealed class UndercurrentOptions
{
    /// <summary>Gets or sets the notification inbox policy.</summary>
    public UserNotificationsOptions Notifications { get; set; } = new();
}