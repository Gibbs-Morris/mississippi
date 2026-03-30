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
}