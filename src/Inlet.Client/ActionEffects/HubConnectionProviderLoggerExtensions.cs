using System;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Logs initial connection checks and their outcomes.
/// </summary>
internal static partial class HubConnectionProviderLoggerExtensions
{
    /// <summary>
    ///     Logs an initial startup failure or cancellation.
    /// </summary>
    /// <param name="logger">The provider's logger.</param>
    /// <param name="level">The severity of the startup outcome.</param>
    /// <param name="exception">The original startup exception.</param>
    /// <param name="elapsedMilliseconds">The elapsed connection-check time.</param>
    [LoggerMessage(
        EventId = 3,
        EventName = "ConnectionStartFailed",
        Message = "Initial SignalR connection failed after {ElapsedMilliseconds} ms")]
    public static partial void ConnectionStartFailed(
        this ILogger logger,
        LogLevel level,
        Exception exception,
        double elapsedMilliseconds
    );

    /// <summary>
    ///     Logs successful completion of the connection check.
    /// </summary>
    /// <param name="logger">The provider's logger.</param>
    /// <param name="connectionState">The resulting transport state.</param>
    /// <param name="elapsedMilliseconds">The elapsed connection-check time.</param>
    [LoggerMessage(
        EventId = 2,
        EventName = "EnsureConnectionCompleted",
        Level = LogLevel.Debug,
        Message = "SignalR connection check completed in {ConnectionState} after {ElapsedMilliseconds} ms")]
    public static partial void EnsureConnectionCompleted(
        this ILogger logger,
        HubConnectionState connectionState,
        double elapsedMilliseconds
    );

    /// <summary>
    ///     Logs entry to the connection check.
    /// </summary>
    /// <param name="logger">The provider's logger.</param>
    /// <param name="connectionState">The initial transport state.</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "EnsureConnectionStarted",
        Level = LogLevel.Debug,
        Message = "Ensuring SignalR connection in {ConnectionState}")]
    public static partial void EnsureConnectionStarted(
        this ILogger logger,
        HubConnectionState connectionState
    );
}