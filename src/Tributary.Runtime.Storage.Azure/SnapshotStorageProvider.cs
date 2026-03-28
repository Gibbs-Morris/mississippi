using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Azure.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     Azure Blob Storage implementation of <see cref="ISnapshotStorageProvider" />.
    /// </summary>
    internal sealed class SnapshotStorageProvider(
        IAzureSnapshotRepository repository,
        ILogger<SnapshotStorageProvider> logger) : ISnapshotStorageProvider
    {
        /// <inheritdoc />
        public string Format => "azure-blob";

        private ILogger<SnapshotStorageProvider> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

        private IAzureSnapshotRepository Repository { get; } = repository ?? throw new ArgumentNullException(nameof(repository));

        /// <inheritdoc />
        public async Task DeleteAllAsync(
            SnapshotStreamKey streamKey,
            CancellationToken cancellationToken = default
        )
        {
            Logger.DeletingAllSnapshots(streamKey);

            try
            {
                await Repository.DeleteAllAsync(streamKey, cancellationToken).ConfigureAwait(false);
                Logger.DeletedAllSnapshots(streamKey);
            }
            catch (Exception exception)
            {
                Logger.DeleteAllSnapshotsFailed(exception, streamKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        )
        {
            Logger.DeletingSnapshot(snapshotKey);

            try
            {
                await Repository.DeleteAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
                Logger.DeletedSnapshot(snapshotKey);
            }
            catch (Exception exception)
            {
                Logger.DeleteSnapshotFailed(exception, snapshotKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PruneAsync(
            SnapshotStreamKey streamKey,
            IReadOnlyCollection<int> retainModuli,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(retainModuli);

            Logger.PruningSnapshots(streamKey, retainModuli.Count);

            try
            {
                await Repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
                Logger.PrunedSnapshots(streamKey);
            }
            catch (Exception exception)
            {
                Logger.PruneSnapshotsFailed(exception, streamKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<SnapshotEnvelope?> ReadAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        )
        {
            Logger.ReadingSnapshot(snapshotKey);

            try
            {
                SnapshotEnvelope? result = await Repository.ReadAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    Logger.SnapshotNotFound(snapshotKey);
                }
                else
                {
                    Logger.SnapshotFound(snapshotKey);
                }

                return result;
            }
            catch (Exception exception)
            {
                Logger.ReadSnapshotFailed(exception, snapshotKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task WriteAsync(
            SnapshotKey snapshotKey,
            SnapshotEnvelope snapshot,
            CancellationToken cancellationToken = default
        )
        {
            Logger.WritingSnapshot(snapshotKey);

            try
            {
                await Repository.WriteAsync(snapshotKey, snapshot, cancellationToken).ConfigureAwait(false);
                Logger.SnapshotWritten(snapshotKey);
            }
            catch (Exception exception)
            {
                Logger.WriteSnapshotFailed(exception, snapshotKey);
                throw;
            }
        }
    }
}
