using Microsoft.Extensions.Logging;


namespace Mississippi.Viaduct.Grains;

/// <summary>
///     Logger extensions for <see cref="SignalRGroupGrain" />.
/// </summary>
internal static partial class SignalRGroupGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Adding connection '{ConnectionId}' to group '{GroupKey}'")]
    public static partial void AddingConnectionToGroup(
        this ILogger logger,
        string connectionId,
        string groupKey
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' added to group '{GroupKey}' (now {ConnectionCount} members)")]
    public static partial void ConnectionAddedToGroup(
        this ILogger logger,
        string connectionId,
        string groupKey,
        int connectionCount
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' already in group '{GroupKey}'")]
    public static partial void ConnectionAlreadyInGroup(
        this ILogger logger,
        string connectionId,
        string groupKey
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' not in group '{GroupKey}'")]
    public static partial void ConnectionNotInGroup(
        this ILogger logger,
        string connectionId,
        string groupKey
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' removed from group '{GroupKey}' (now {ConnectionCount} members)")]
    public static partial void ConnectionRemovedFromGroup(
        this ILogger logger,
        string connectionId,
        string groupKey,
        int connectionCount
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Group grain activated for '{GroupKey}' with {ConnectionCount} connections")]
    public static partial void GroupGrainActivated(
        this ILogger logger,
        string groupKey,
        int connectionCount
    );

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Group '{GroupKey}' is now empty, deactivating")]
    public static partial void GroupNowEmpty(
        this ILogger logger,
        string groupKey
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Removing connection '{ConnectionId}' from group '{GroupKey}'")]
    public static partial void RemovingConnectionFromGroup(
        this ILogger logger,
        string connectionId,
        string groupKey
    );

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Sending '{MethodName}' to group '{GroupKey}' ({ConnectionCount} connections)")]
    public static partial void SendingToGroup(
        this ILogger logger,
        string groupKey,
        string methodName,
        int connectionCount
    );

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Sent '{MethodName}' to group '{GroupKey}' ({ConnectionCount} connections)")]
    public static partial void SentToGroup(
        this ILogger logger,
        string groupKey,
        string methodName,
        int connectionCount
    );
}