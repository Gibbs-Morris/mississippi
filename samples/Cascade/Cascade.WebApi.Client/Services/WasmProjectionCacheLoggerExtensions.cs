using Microsoft.Extensions.Logging;


namespace Cascade.WebApi.Client.Services;

/// <summary>
///     High-performance logger extensions for <see cref="WasmProjectionCache" />.
/// </summary>
internal static partial class WasmProjectionCacheLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to fetch projection: {Error}")]
    public static partial void FetchProjectionFailed(
        this ILogger logger,
        string error
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Fetching projection {ProjectionType}/{EntityId}")]
    public static partial void FetchingProjection(
        this ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Projection update received: {ProjectionType}/{EntityId} version {NewVersion}")]
    public static partial void ProjectionUpdateReceived(
        this ILogger logger,
        string projectionType,
        string entityId,
        long newVersion
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "SignalR connection established")]
    public static partial void SignalRConnectionEstablished(
        this ILogger logger
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "SignalR reconnected with connection ID: {ConnectionId}")]
    public static partial void SignalRReconnected(
        this ILogger logger,
        string? connectionId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Starting SignalR connection")]
    public static partial void StartingSignalRConnection(
        this ILogger logger
    );
}
