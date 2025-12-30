using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     High-performance logging extensions for <see cref="BrookSliceReaderGrain" />.
/// </summary>
internal static partial class BrookSliceReaderGrainLoggerExtensions
{
    /// <summary>
    ///     Logs that the cache has been populated from storage.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="brookRangeKey">The brook range key for this slice.</param>
    /// <param name="eventCount">The number of events loaded into the cache.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "BrookSliceReaderGrain cache populated for slice '{BrookRangeKey}' with {EventCount} events")]
    public static partial void SliceCachePopulated(
        this ILogger logger,
        BrookRangeKey brookRangeKey,
        int eventCount
    );

    /// <summary>
    ///     Logs that the grain has been activated and is populating its cache from storage.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="brookRangeKey">The brook range key for this slice.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "BrookSliceReaderGrain activating for slice '{BrookRangeKey}', populating cache from storage")]
    public static partial void SliceGrainActivating(
        this ILogger logger,
        BrookRangeKey brookRangeKey
    );
}