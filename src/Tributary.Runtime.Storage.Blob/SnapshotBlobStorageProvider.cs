using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Blob-backed implementation placeholder for <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotBlobStorageProvider : ISnapshotStorageProvider
{
    private const string IncrementNotImplementedMessage =
        "Blob snapshot storage behavior is not implemented until increment 4.";

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobStorageProvider" /> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobStorageProvider(
        ILogger<SnapshotBlobStorageProvider> logger
    ) =>
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Format => "azure-blob";

    private ILogger<SnapshotBlobStorageProvider> Logger { get; }

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.SnapshotOperationNotImplemented(nameof(DeleteAllAsync));
        throw new NotSupportedException(IncrementNotImplementedMessage);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.SnapshotOperationNotImplemented(nameof(DeleteAsync));
        throw new NotSupportedException(IncrementNotImplementedMessage);
    }

    /// <inheritdoc />
    public Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        Logger.SnapshotOperationNotImplemented(nameof(PruneAsync));
        throw new NotSupportedException(IncrementNotImplementedMessage);
    }

    /// <inheritdoc />
    public Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.SnapshotOperationNotImplemented(nameof(ReadAsync));
        throw new NotSupportedException(IncrementNotImplementedMessage);
    }

    /// <inheritdoc />
    public Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        Logger.SnapshotOperationNotImplemented(nameof(WriteAsync));
        throw new NotSupportedException(IncrementNotImplementedMessage);
    }
}