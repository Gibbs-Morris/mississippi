using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Logger extensions for <see cref="ReplicaSinkLatestStateProcessor" />.
/// </summary>
internal static partial class ReplicaSinkLatestStateProcessorLoggerExtensions
{
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message =
            "Replica sink lane '{DeliveryKey}' persisted dead-letter state for source position {SourcePosition} with code '{FailureCode}'.")]
    public static partial void DeadLetterPersisted(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        string failureCode
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message =
            "Replica sink '{SinkKey}' quarantined lane '{DeliveryKey}' after dead-letter persistence failed.")]
    public static partial void DeadLetterStoreQuarantined(
        this ILogger logger,
        string sinkKey,
        string deliveryKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Replica sink lane '{DeliveryKey}' checkpointed committed source position {SourcePosition}.")]
    public static partial void DeliveryCheckpointed(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message =
            "Replica sink desired watermark advanced for projection '{ProjectionType}' entity '{EntityId}' to source position {SourcePosition}.")]
    public static partial void DesiredWatermarkAdvanced(
        this ILogger logger,
        string projectionType,
        string entityId,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message =
            "Replica sink lane '{DeliveryKey}' persisted retry state for source position {SourcePosition} with code '{FailureCode}' until {NextRetryAtUtc}.")]
    public static partial void RetryPersisted(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        string failureCode,
        DateTimeOffset nextRetryAtUtc
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message =
            "Replica sink lane '{DeliveryKey}' rejected rewind to requested source position {RequestedSourcePosition}.")]
    public static partial void RewindRejected(
        this ILogger logger,
        string deliveryKey,
        long requestedSourcePosition
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message =
            "Replica sink source state was not yet available for projection '{ProjectionType}' entity '{EntityId}' at source position {SourcePosition}.")]
    public static partial void SourceStateUnavailable(
        this ILogger logger,
        string projectionType,
        string entityId,
        long sourcePosition,
        Exception exception
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message =
            "Replica sink source state read failed for projection '{ProjectionType}' entity '{EntityId}' at source position {SourcePosition}.")]
    public static partial void SourceStateReadFailed(
        this ILogger logger,
        string projectionType,
        string entityId,
        long sourcePosition,
        Exception exception
    );

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message =
            "Replica sink lane '{DeliveryKey}' failed to materialize payload for source position {SourcePosition}.")]
    public static partial void ProjectionMappingFailed(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        Exception exception
    );

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Warning,
        Message =
            "Replica sink lane '{DeliveryKey}' scheduled retry for source position {SourcePosition} with code '{FailureCode}'.")]
    public static partial void ProviderWriteRetryScheduled(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        string failureCode,
        Exception exception
    );

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message =
            "Replica sink lane '{DeliveryKey}' moved source position {SourcePosition} to dead letter with code '{FailureCode}'.")]
    public static partial void ProviderWriteDeadLettered(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        string failureCode,
        Exception exception
    );

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message =
            "Replica sink lane '{DeliveryKey}' failed unexpectedly while writing source position {SourcePosition}.")]
    public static partial void ProviderWriteFailed(
        this ILogger logger,
        string deliveryKey,
        long sourcePosition,
        Exception exception
    );
}