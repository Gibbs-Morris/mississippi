using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Brooks;

/// <summary>
///     High-performance logging extensions for <see cref="EventBrookReader" />.
/// </summary>
internal static partial class EventBrookReaderLoggerExtensions
{
    /// <summary>
    ///     Logs when the read operation completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookRangeKey">The brook range key.</param>
    /// <param name="eventCount">The number of events read.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Completed reading {EventCount} events from brook range '{BrookRangeKey}'")]
    public static partial void ReadCompleted(
        this ILogger logger,
        BrookRangeKey brookRangeKey,
        int eventCount
    );

    /// <summary>
    ///     Logs when starting to read events from a brook range.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookRangeKey">The brook range key.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Reading events from brook range '{BrookRangeKey}'")]
    public static partial void ReadingEvents(
        this ILogger logger,
        BrookRangeKey brookRangeKey
    );

    /// <summary>
    ///     Logs when an event is yielded from the reader.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="eventPosition">The position of the event.</param>
    /// <param name="eventType">The type of the event.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Yielding event from brook '{BrookKey}' at position {EventPosition} (type: {EventType})")]
    public static partial void YieldingEvent(
        this ILogger logger,
        string brookKey,
        long eventPosition,
        string eventType
    );
}