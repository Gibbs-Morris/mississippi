using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Logging helpers for the aggregate Cosmos delivery-state store.
/// </summary>
internal static partial class CosmosReplicaSinkDeliveryStateStoreLoggerExtensions
{
    [LoggerMessage(
        EventId = 4200,
        Level = LogLevel.Debug,
        Message = "Reading Cosmos replica sink state for delivery '{DeliveryKey}' routed to sink '{SinkKey}'.")]
    internal static partial void LogReadingState(
        this ILogger logger,
        string deliveryKey,
        string sinkKey
    );

    [LoggerMessage(
        EventId = 4201,
        Level = LogLevel.Debug,
        Message = "Writing Cosmos replica sink state for delivery '{DeliveryKey}' routed to sink '{SinkKey}'.")]
    internal static partial void LogWritingState(
        this ILogger logger,
        string deliveryKey,
        string sinkKey
    );

    [LoggerMessage(
        EventId = 4202,
        Level = LogLevel.Debug,
        Message = "Reading due retries across {SinkCount} Cosmos replica sinks with cutoff '{DueAtUtc}' and max count {MaxCount}.")]
    internal static partial void LogReadingDueRetries(
        this ILogger logger,
        int sinkCount,
        string dueAtUtc,
        int maxCount
    );

    [LoggerMessage(
        EventId = 4203,
        Level = LogLevel.Debug,
        Message = "Read {ResultCount} due retries across Cosmos replica sinks.")]
    internal static partial void LogReadDueRetries(
        this ILogger logger,
        int resultCount
    );

    [LoggerMessage(
        EventId = 4204,
        Level = LogLevel.Debug,
        Message = "Reading dead-letter page across {SinkCount} Cosmos replica sinks with offset {Offset} and page size {PageSize}.")]
    internal static partial void LogReadingDeadLetters(
        this ILogger logger,
        int sinkCount,
        int offset,
        int pageSize
    );

    [LoggerMessage(
        EventId = 4205,
        Level = LogLevel.Debug,
        Message = "Read {ResultCount} dead-letter items across Cosmos replica sinks. More results: {HasMore}.")]
    internal static partial void LogReadDeadLetters(
        this ILogger logger,
        int resultCount,
        bool hasMore
    );
}
