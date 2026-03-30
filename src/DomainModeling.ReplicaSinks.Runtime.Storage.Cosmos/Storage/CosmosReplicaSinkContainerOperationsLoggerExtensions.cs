using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Logging helpers for Cosmos SDK interactions performed by the replica sink storage layer.
/// </summary>
internal static partial class CosmosReplicaSinkContainerOperationsLoggerExtensions
{
    [LoggerMessage(
        EventId = 4403,
        Level = LogLevel.Warning,
        Message =
            "Cosmos replica sink '{SinkKey}' container '{DatabaseId}/{ContainerId}' is missing for provisioning mode '{ProvisioningMode}'.")]
    internal static partial void LogContainerMissing(
        this ILogger logger,
        Exception exception,
        string sinkKey,
        string databaseId,
        string containerId,
        string provisioningMode
    );

    [LoggerMessage(
        EventId = 4402,
        Level = LogLevel.Information,
        Message = "Provisioned Cosmos replica sink '{SinkKey}' container '{DatabaseId}/{ContainerId}'.")]
    internal static partial void LogContainerProvisioned(
        this ILogger logger,
        string sinkKey,
        string databaseId,
        string containerId
    );

    [LoggerMessage(
        EventId = 4401,
        Level = LogLevel.Information,
        Message = "Validated Cosmos replica sink '{SinkKey}' container '{DatabaseId}/{ContainerId}'.")]
    internal static partial void LogContainerValidated(
        this ILogger logger,
        string sinkKey,
        string databaseId,
        string containerId
    );

    [LoggerMessage(
        EventId = 4404,
        Level = LogLevel.Debug,
        Message = "Creating Cosmos replica sink '{SinkKey}' target '{TargetName}'.")]
    internal static partial void LogCreatingTarget(
        this ILogger logger,
        string sinkKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4413,
        Level = LogLevel.Debug,
        Message = "Cosmos replica sink '{SinkKey}' durable state for delivery '{DeliveryKey}' was not found.")]
    internal static partial void LogDeliveryStateNotFound(
        this ILogger logger,
        Exception exception,
        string sinkKey,
        string deliveryKey
    );

    [LoggerMessage(
        EventId = 4415,
        Level = LogLevel.Debug,
        Message = "Wrote Cosmos replica sink '{SinkKey}' durable state for delivery '{DeliveryKey}'.")]
    internal static partial void LogDeliveryStateWritten(
        this ILogger logger,
        string sinkKey,
        string deliveryKey
    );

    [LoggerMessage(
        EventId = 4400,
        Level = LogLevel.Debug,
        Message =
            "Ensuring Cosmos replica sink '{SinkKey}' container '{DatabaseId}/{ContainerId}' with provisioning mode '{ProvisioningMode}'.")]
    internal static partial void LogEnsuringContainer(
        this ILogger logger,
        string sinkKey,
        string databaseId,
        string containerId,
        string provisioningMode
    );

    [LoggerMessage(
        EventId = 4407,
        Level = LogLevel.Debug,
        Message = "Inspecting Cosmos replica sink '{SinkKey}' target '{TargetName}'.")]
    internal static partial void LogInspectingTarget(
        this ILogger logger,
        string sinkKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4416,
        Level = LogLevel.Debug,
        Message = "Completed Cosmos replica sink '{SinkKey}' query '{QueryName}' with {ResultCount} results.")]
    internal static partial void LogQueryCompleted(
        this ILogger logger,
        string sinkKey,
        string queryName,
        int resultCount
    );

    [LoggerMessage(
        EventId = 4406,
        Level = LogLevel.Debug,
        Message = "Cosmos replica sink '{SinkKey}' target '{TargetName}' already exists.")]
    internal static partial void LogTargetAlreadyExists(
        this ILogger logger,
        Exception exception,
        string sinkKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4405,
        Level = LogLevel.Information,
        Message = "Created Cosmos replica sink '{SinkKey}' target '{TargetName}'.")]
    internal static partial void LogTargetCreated(
        this ILogger logger,
        string sinkKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4410,
        Level = LogLevel.Debug,
        Message = "Cosmos replica sink '{SinkKey}' delivery '{DeliveryKey}' for target '{TargetName}' was not found.")]
    internal static partial void LogTargetDeliveryNotFound(
        this ILogger logger,
        Exception exception,
        string sinkKey,
        string targetName,
        string deliveryKey
    );

    [LoggerMessage(
        EventId = 4412,
        Level = LogLevel.Debug,
        Message =
            "Wrote Cosmos replica sink '{SinkKey}' delivery '{DeliveryKey}' for target '{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogTargetDeliveryWritten(
        this ILogger logger,
        string sinkKey,
        string targetName,
        string deliveryKey,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 4408,
        Level = LogLevel.Debug,
        Message =
            "Inspected Cosmos replica sink '{SinkKey}' target '{TargetName}' (Exists: {TargetExists}, WriteCount: {WriteCount}).")]
    internal static partial void LogTargetInspected(
        this ILogger logger,
        string sinkKey,
        string targetName,
        bool targetExists,
        long writeCount
    );

    [LoggerMessage(
        EventId = 4409,
        Level = LogLevel.Debug,
        Message = "Cosmos replica sink '{SinkKey}' target marker '{TargetName}' was not found.")]
    internal static partial void LogTargetMarkerNotFound(
        this ILogger logger,
        Exception exception,
        string sinkKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 4414,
        Level = LogLevel.Debug,
        Message = "Writing Cosmos replica sink '{SinkKey}' durable state for delivery '{DeliveryKey}'.")]
    internal static partial void LogWritingDeliveryState(
        this ILogger logger,
        string sinkKey,
        string deliveryKey
    );

    [LoggerMessage(
        EventId = 4411,
        Level = LogLevel.Debug,
        Message =
            "Writing Cosmos replica sink '{SinkKey}' delivery '{DeliveryKey}' for target '{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogWritingTargetDelivery(
        this ILogger logger,
        string sinkKey,
        string targetName,
        string deliveryKey,
        long sourcePosition
    );
}