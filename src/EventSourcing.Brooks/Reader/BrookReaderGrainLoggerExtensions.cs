using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Reader;

/// <summary>
///     High-performance logging extensions for <see cref="BrookReaderGrain" />.
/// </summary>
internal static partial class BrookReaderGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when the reader grain is activated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "BrookReaderGrain activated for brook '{BrookKey}'")]
    public static partial void Activated(
        this ILogger logger,
        BrookKey brookKey
    );

    /// <summary>
    ///     Logs when the brook is empty (no events to read).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Brook '{BrookKey}' is empty, returning no events")]
    public static partial void BrookEmpty(
        this ILogger logger,
        BrookKey brookKey
    );

    /// <summary>
    ///     Logs when the reader grain is being deactivated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "BrookReaderGrain deactivating for brook '{BrookKey}'")]
    public static partial void Deactivating(
        this ILogger logger,
        BrookKey brookKey
    );

    /// <summary>
    ///     Logs when a batch read operation completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="eventCount">The number of events read.</param>
    /// <param name="sliceCount">The number of slices used for parallel reads.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Read {EventCount} events from brook '{BrookKey}' using {SliceCount} slices ({ElapsedMs}ms)")]
    public static partial void EventsBatchRead(
        this ILogger logger,
        BrookKey brookKey,
        int eventCount,
        int sliceCount,
        long elapsedMs
    );

    /// <summary>
    ///     Logs when starting a batch read operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="fromPosition">The starting position (null for beginning).</param>
    /// <param name="toPosition">The ending position (null for current end).</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Reading events batch from brook '{BrookKey}' (from: {FromPosition}, to: {ToPosition})")]
    public static partial void ReadingEventsBatch(
        this ILogger logger,
        BrookKey brookKey,
        long? fromPosition,
        long? toPosition
    );
}