using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Writer;

/// <summary>
///     High-performance logging extensions for <see cref="BrookWriterGrain" />.
/// </summary>
internal static partial class BrookWriterGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when the writer grain is activated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "BrookWriterGrain activated for brook '{BrookKey}'")]
    public static partial void Activated(
        this ILogger logger,
        BrookKey brookKey
    );

    /// <summary>
    ///     Logs when an error occurs during event append.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="eventCount">The number of events that failed to append.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Failed to append {EventCount} events to brook '{BrookKey}'")]
    public static partial void AppendFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookKey,
        int eventCount
    );

    /// <summary>
    ///     Logs when events are being appended to the brook.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="eventCount">The number of events being appended.</param>
    /// <param name="expectedPosition">The expected cursor position (null for first write).</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Appending {EventCount} events to brook '{BrookKey}' (expected position: {ExpectedPosition})")]
    public static partial void AppendingEvents(
        this ILogger logger,
        BrookKey brookKey,
        int eventCount,
        long? expectedPosition
    );

    /// <summary>
    ///     Logs when events have been successfully appended to the brook.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="eventCount">The number of events appended.</param>
    /// <param name="newPosition">The new cursor position after append.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Appended {EventCount} events to brook '{BrookKey}', new position: {NewPosition} ({ElapsedMs}ms)")]
    public static partial void EventsAppended(
        this ILogger logger,
        BrookKey brookKey,
        int eventCount,
        long newPosition,
        long elapsedMs
    );

    /// <summary>
    ///     Logs when publishing cursor moved event to the stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="newPosition">The new cursor position.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Publishing cursor moved event for brook '{BrookKey}' to position {NewPosition}")]
    public static partial void PublishingCursorMoved(
        this ILogger logger,
        BrookKey brookKey,
        long newPosition
    );
}