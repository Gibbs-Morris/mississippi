using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     High-performance logging extensions for snapshot retention policy startup.
/// </summary>
internal static partial class SnapshotRetentionPolicyLoggerExtensions
{
    [LoggerMessage(
        2,
        LogLevel.Warning,
        "Snapshot retention is configured to persist every reconstructed snapshot; use this debugging mode only when its storage overhead is acceptable")]
    public static partial void PersistAllSnapshotsEnabled(
        this ILogger logger
    );

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Snapshot retention policy configured with default modulus {DefaultRetainModulus}, {OverrideCount} state overrides, persist-all {ShouldPersistAllSnapshots}")]
    public static partial void PolicyConfigured(
        this ILogger logger,
        int defaultRetainModulus,
        bool shouldPersistAllSnapshots,
        int overrideCount
    );
}