using System;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     High-performance logging extensions for the Brooks Azure provider.
/// </summary>
internal static partial class BrookStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs the start of an append operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="eventCount">The number of events requested.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Appending {EventCount} event(s) to Brooks Azure brook '{BrookId}'")]
    public static partial void AppendingEvents(
        this ILogger logger,
        BrookKey brookId,
        int eventCount
    );

    /// <summary>
    ///     Logs successful completion of an append operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The committed cursor position.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Completed Brooks Azure append for brook '{BrookId}' at cursor position {Position}")]
    public static partial void AppendEventsCompleted(
        this ILogger logger,
        BrookKey brookId,
        long position
    );

    /// <summary>
    ///     Logs append failures.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Brooks Azure append failed for brook '{BrookId}'")]
    public static partial void AppendEventsFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs the start of a cursor read.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Reading Brooks Azure cursor position for brook '{BrookId}'")]
    public static partial void ReadingCursorPosition(
        this ILogger logger,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs successful completion of a cursor read.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The resolved cursor position.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Resolved Brooks Azure cursor position {Position} for brook '{BrookId}'")]
    public static partial void ReadCursorPositionCompleted(
        this ILogger logger,
        BrookKey brookId,
        long position
    );

    /// <summary>
    ///     Logs cursor read failures.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Brooks Azure cursor read failed for brook '{BrookId}'")]
    public static partial void ReadCursorPositionFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs the start of an event read.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookRange">The requested range.</param>
    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Reading Brooks Azure events for range '{BrookRange}'")]
    public static partial void ReadingEvents(
        this ILogger logger,
        BrookRangeKey brookRange
    );

    /// <summary>
    ///     Logs the resolved committed event count for a read.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookRange">The committed range used for the read.</param>
    /// <param name="eventCount">The number of committed events available.</param>
    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Resolved Brooks Azure committed read range '{BrookRange}' with {EventCount} event(s)")]
    public static partial void ReadEventsResolvedRange(
        this ILogger logger,
        BrookRangeKey brookRange,
        long eventCount
    );

    /// <summary>
    ///     Logs event read failures.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="brookRange">The requested or resolved range.</param>
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Brooks Azure event read failed for range '{BrookRange}'")]
    public static partial void ReadEventsFailed(
        this ILogger logger,
        Exception exception,
        BrookRangeKey brookRange
    );
}