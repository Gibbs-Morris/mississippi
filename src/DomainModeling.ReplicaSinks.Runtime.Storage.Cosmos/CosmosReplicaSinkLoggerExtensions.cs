#if false
using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Logging helpers for the Cosmos-backed replica sink provider.
/// </summary>
internal static partial class CosmosReplicaSinkProviderLoggerExtensions
{
    [LoggerMessage(
        EventId = 4100,
        Level = LogLevel.Debug,
        Message =
 "Ensuring Cosmos replica sink '{SinkKey}' target '{ClientKey}:{TargetName}' with provisioning mode '{ProvisioningMode}'.")]
    internal static partial void LogEnsuringTarget(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        string provisioningMode
    );

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Provisioned Cosmos replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogTargetProvisioned(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Debug,
        Message = "Validated existing Cosmos replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogTargetValidated(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4103,
        Level = LogLevel.Debug,
        Message = "Inspecting Cosmos replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogInspectingTarget(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4104,
        Level = LogLevel.Debug,
        Message =
 "Inspected Cosmos replica sink '{SinkKey}' target '{ClientKey}:{TargetName}' (Exists: {TargetExists}, WriteCount: {WriteCount}).")]
    internal static partial void LogTargetInspected(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        bool targetExists,
        long writeCount
    );

    [LoggerMessage(
        EventId = 4105,
        Level = LogLevel.Debug,
        Message =
 "Writing Cosmos replica sink '{SinkKey}' delivery '{DeliveryKey}' to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogWritingReplica(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        string deliveryKey,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 4106,
        Level = LogLevel.Information,
        Message =
 "Applied Cosmos replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaApplied(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 4107,
        Level = LogLevel.Debug,
        Message =
 "Ignored duplicate Cosmos replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaDuplicateIgnored(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 4108,
        Level = LogLevel.Debug,
        Message =
 "Ignored superseded Cosmos replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaSupersededIgnored(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );
}

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
        Message =
 "Reading due retries across {SinkCount} Cosmos replica sinks with cutoff '{DueAtUtc}' and max count {MaxCount}.")]
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
        Message =
 "Reading dead-letter page across {SinkCount} Cosmos replica sinks with offset {Offset} and page size {PageSize}.")]
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

/// <summary>
///     Logging helpers for Cosmos replica sink container initialization.
/// </summary>
internal static partial class CosmosReplicaSinkContainerInitializerLoggerExtensions
{
    [LoggerMessage(
        EventId = 4300,
        Level = LogLevel.Information,
        Message = "Initializing Cosmos replica sink containers for {SinkCount} sink registrations.")]
    internal static partial void LogInitializingContainers(
        this ILogger logger,
        int sinkCount
    );

    [LoggerMessage(
        EventId = 4301,
        Level = LogLevel.Information,
        Message = "Initialized Cosmos replica sink containers for {SinkCount} sink registrations.")]
    internal static partial void LogInitializedContainers(
        this ILogger logger,
        int sinkCount
    );
}
#endif