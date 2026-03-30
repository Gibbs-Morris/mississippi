using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     Logging helpers for the bootstrap replica sink provider.
/// </summary>
internal static partial class BootstrapReplicaSinkProviderLoggerExtensions
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message =
            "Ensuring bootstrap replica sink '{SinkKey}' target '{ClientKey}:{TargetName}' with provisioning mode '{ProvisioningMode}'.")]
    internal static partial void LogEnsuringTarget(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        string provisioningMode
    );

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message =
            "Applied bootstrap replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaApplied(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message =
            "Ignored duplicate bootstrap replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaDuplicateIgnored(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Inspected bootstrap replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogReplicaInspected(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message =
            "Ignored superseded bootstrap replica sink '{SinkKey}' write to '{ClientKey}:{TargetName}' at source position {SourcePosition}.")]
    internal static partial void LogReplicaSupersededIgnored(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName,
        long sourcePosition
    );

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Provisioned bootstrap replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogTargetProvisioned(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Validated existing bootstrap replica sink '{SinkKey}' target '{ClientKey}:{TargetName}'.")]
    internal static partial void LogTargetValidated(
        this ILogger logger,
        string sinkKey,
        string clientKey,
        string targetName
    );
}