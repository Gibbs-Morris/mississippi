using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Blob-backed implementation of <see cref="ISnapshotBlobRepository" />.
/// </summary>
internal sealed class SnapshotBlobRepository : ISnapshotBlobRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobRepository" /> class.
    /// </summary>
    /// <param name="operations">The Blob operations abstraction.</param>
    /// <param name="codec">The snapshot document codec.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobRepository(
        ISnapshotBlobOperations operations,
        ISnapshotBlobCodec codec,
        ILogger<SnapshotBlobRepository> logger
    )
    {
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        Codec = codec ?? throw new ArgumentNullException(nameof(codec));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ISnapshotBlobCodec Codec { get; }

    private ILogger<SnapshotBlobRepository> Logger { get; }

    private ISnapshotBlobOperations Operations { get; }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        string prefix = SnapshotBlobPath.BuildStreamPrefix(streamKey);
        Logger.DeletingAllSnapshots(prefix);
        await foreach (string blobName in Operations.ListBlobNamesAsync(prefix, cancellationToken))
        {
            if (!SnapshotBlobPath.TryParseVersionFromBlobName(blobName, streamKey, out long _))
            {
                continue;
            }

            await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false);
            Logger.SnapshotDeleted(blobName);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false);
        Logger.SnapshotDeleted(blobName);
    }

    /// <inheritdoc />
    public async Task<int> PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        string prefix = SnapshotBlobPath.BuildStreamPrefix(streamKey);
        Logger.PruningSnapshots(prefix, retainModuli.Count);
        List<(string BlobName, long Version)> versions = [];
        await foreach (string blobName in Operations.ListBlobNamesAsync(prefix, cancellationToken))
        {
            if (SnapshotBlobPath.TryParseVersionFromBlobName(blobName, streamKey, out long version))
            {
                versions.Add((blobName, version));
            }
        }

        if (versions.Count == 0)
        {
            return 0;
        }

        long maxVersion = versions.Max(item => item.Version);
        HashSet<long> retainedVersions = new(
            versions.Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                .Select(item => item.Version));
        retainedVersions.Add(maxVersion);
        int deletedCount = 0;
        foreach ((string blobName, long version) in versions)
        {
            if (retainedVersions.Contains(version))
            {
                continue;
            }

            if (await Operations.DeleteIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false))
            {
                deletedCount++;
                Logger.SnapshotDeleted(blobName);
            }
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        try
        {
            BinaryData? documentPayload =
                await Operations.DownloadAsync(blobName, cancellationToken).ConfigureAwait(false);
            if (documentPayload is null)
            {
                Logger.SnapshotNotFound(blobName);
                return null;
            }

            SnapshotEnvelope snapshot = Codec.Decode(snapshotKey, documentPayload);
            Logger.SnapshotRead(blobName);
            return snapshot;
        }
        catch (InvalidDataException exception)
        {
            Logger.InvalidSnapshotDocument(blobName, exception.Message, exception);
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
        ArgumentNullException.ThrowIfNull(snapshot);
        BinaryData document = Codec.Encode(snapshotKey, snapshot);
        string blobName = SnapshotBlobPath.BuildSnapshotBlobName(snapshotKey);
        await Operations.UploadAsync(blobName, document, cancellationToken).ConfigureAwait(false);
        Logger.SnapshotUploaded(blobName);
    }
}