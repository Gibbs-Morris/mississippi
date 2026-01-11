using System;

using Microsoft.Extensions.Logging;


namespace Cascade.Web.Server.Services;

/// <summary>
///     High-performance logger extensions for <see cref="StreamBridgeService" />.
/// </summary>
internal static partial class StreamBridgeServiceLoggerExtensions
{
    /// <summary>
    ///     Logs when the stream bridge service is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "StreamBridgeService starting, connecting to Orleans cluster...")]
    public static partial void LogStreamBridgeStarting(
        this ILogger<StreamBridgeService> logger
    );

    /// <summary>
    ///     Logs when the stream bridge has subscribed to a stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamNamespace">The stream namespace.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "StreamBridgeService subscribed to stream {StreamNamespace}/{StreamKey}")]
    public static partial void LogStreamBridgeSubscribed(
        this ILogger<StreamBridgeService> logger,
        string streamNamespace,
        string streamKey
    );

    /// <summary>
    ///     Logs when the stream bridge service is stopping.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Information,
        Message = "StreamBridgeService stopping...")]
    public static partial void LogStreamBridgeStopping(
        this ILogger<StreamBridgeService> logger
    );

    /// <summary>
    ///     Logs when a message is received from the stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sender">The message sender.</param>
    /// <param name="content">The message content.</param>
    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Stream message received from {Sender}: {Content}")]
    public static partial void LogStreamMessageReceived(
        this ILogger<StreamBridgeService> logger,
        string sender,
        string content
    );

    /// <summary>
    ///     Logs when an error occurs in the stream bridge.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Error,
        Message = "StreamBridgeService encountered an error")]
    public static partial void LogStreamBridgeError(
        this ILogger<StreamBridgeService> logger,
        Exception exception
    );
}
