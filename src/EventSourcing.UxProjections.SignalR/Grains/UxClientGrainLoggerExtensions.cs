using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Logger extensions for <see cref="UxClientGrain" />.
/// </summary>
internal static partial class UxClientGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Client '{ConnectionId}' connected to hub '{HubName}' on server '{ServerId}'")]
    public static partial void ClientConnected(
        this ILogger logger,
        string connectionId,
        string hubName,
        string serverId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Client '{ConnectionId}' connecting to hub '{HubName}' on server '{ServerId}'")]
    public static partial void ClientConnecting(
        this ILogger logger,
        string connectionId,
        string hubName,
        string serverId
    );

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Client '{ConnectionId}' disconnected")]
    public static partial void ClientDisconnected(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Client '{ConnectionId}' disconnecting")]
    public static partial void ClientDisconnecting(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Client grain activated for connection '{ConnectionId}'")]
    public static partial void ClientGrainActivated(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Sending message '{MethodName}' to client '{ConnectionId}'")]
    public static partial void SendingMessage(
        this ILogger logger,
        string connectionId,
        string methodName
    );
}