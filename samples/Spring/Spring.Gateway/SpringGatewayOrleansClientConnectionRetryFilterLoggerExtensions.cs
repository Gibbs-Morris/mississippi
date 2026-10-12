using System;

using Microsoft.Extensions.Logging;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Provides diagnostics for <see cref="SpringGatewayOrleansClientConnectionRetryFilter" />.
/// </summary>
internal static partial class SpringGatewayOrleansClientConnectionRetryFilterLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Spring gateway exhausted its {MaxRetries} Orleans connection retries.")]
    public static partial void ConnectionRetriesExhausted(
        this ILogger logger,
        int maxRetries
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Spring gateway Orleans connection retry was canceled.")]
    public static partial void ConnectionRetryCanceled(
        this ILogger logger,
        OperationCanceledException exception
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Spring gateway Orleans connection retry decision: {ShouldRetry}.")]
    public static partial void ConnectionRetryCompleted(
        this ILogger logger,
        bool shouldRetry
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Spring gateway is evaluating an Orleans startup connection failure.")]
    public static partial void ConnectionRetryStarted(
        this ILogger logger,
        Exception? exception
    );
}