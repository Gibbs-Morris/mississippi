using System;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     High-performance logging extensions for the Tributary Azure snapshot provider.
    /// </summary>
    internal static partial class SnapshotStorageProviderLoggerExtensions
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Deleting all Tributary Azure snapshots for stream '{StreamKey}'")]
        internal static partial void DeletingAllSnapshots(
            this ILogger logger,
            SnapshotStreamKey streamKey
        );

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "Completed deleting all Tributary Azure snapshots for stream '{StreamKey}'")]
        internal static partial void DeletedAllSnapshots(
            this ILogger logger,
            SnapshotStreamKey streamKey
        );

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Error,
            Message = "Deleting all Tributary Azure snapshots failed for stream '{StreamKey}'")]
        internal static partial void DeleteAllSnapshotsFailed(
            this ILogger logger,
            Exception exception,
            SnapshotStreamKey streamKey
        );

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Information,
            Message = "Deleting Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void DeletingSnapshot(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Information,
            Message = "Completed deleting Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void DeletedSnapshot(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 6,
            Level = LogLevel.Error,
            Message = "Deleting Tributary Azure snapshot failed for key '{SnapshotKey}'")]
        internal static partial void DeleteSnapshotFailed(
            this ILogger logger,
            Exception exception,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 7,
            Level = LogLevel.Information,
            Message = "Pruning Tributary Azure snapshots for stream '{StreamKey}' using {RetainModuliCount} retain modulus value(s)")]
        internal static partial void PruningSnapshots(
            this ILogger logger,
            SnapshotStreamKey streamKey,
            int retainModuliCount
        );

        [LoggerMessage(
            EventId = 8,
            Level = LogLevel.Information,
            Message = "Completed pruning Tributary Azure snapshots for stream '{StreamKey}'")]
        internal static partial void PrunedSnapshots(
            this ILogger logger,
            SnapshotStreamKey streamKey
        );

        [LoggerMessage(
            EventId = 9,
            Level = LogLevel.Error,
            Message = "Pruning Tributary Azure snapshots failed for stream '{StreamKey}'")]
        internal static partial void PruneSnapshotsFailed(
            this ILogger logger,
            Exception exception,
            SnapshotStreamKey streamKey
        );

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "Reading Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void ReadingSnapshot(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 11,
            Level = LogLevel.Information,
            Message = "Found Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void SnapshotFound(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 12,
            Level = LogLevel.Information,
            Message = "Tributary Azure snapshot '{SnapshotKey}' was not found")]
        internal static partial void SnapshotNotFound(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 13,
            Level = LogLevel.Error,
            Message = "Reading Tributary Azure snapshot failed for key '{SnapshotKey}'")]
        internal static partial void ReadSnapshotFailed(
            this ILogger logger,
            Exception exception,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 14,
            Level = LogLevel.Information,
            Message = "Writing Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void WritingSnapshot(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 15,
            Level = LogLevel.Information,
            Message = "Completed writing Tributary Azure snapshot '{SnapshotKey}'")]
        internal static partial void SnapshotWritten(
            this ILogger logger,
            SnapshotKey snapshotKey
        );

        [LoggerMessage(
            EventId = 16,
            Level = LogLevel.Error,
            Message = "Writing Tributary Azure snapshot failed for key '{SnapshotKey}'")]
        internal static partial void WriteSnapshotFailed(
            this ILogger logger,
            Exception exception,
            SnapshotKey snapshotKey
        );
    }
}
