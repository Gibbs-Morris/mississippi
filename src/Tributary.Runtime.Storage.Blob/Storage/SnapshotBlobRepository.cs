using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Blob-backed implementation of <see cref="ISnapshotBlobRepository" />.
/// </summary>
internal sealed class SnapshotBlobRepository : ISnapshotBlobRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobRepository" /> class.
    /// </summary>
    /// <param name="containerOperations">The Blob container operations abstraction.</param>
    /// <param name="storageToEnvelopeMapper">Maps storage models to snapshot envelopes.</param>
    /// <param name="writeModelToStorageMapper">Maps write models to storage models.</param>
    /// <param name="options">The Blob snapshot storage options.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobRepository(
        ISnapshotBlobContainerOperations containerOperations,
        IMapper<SnapshotBlobStorageModel, SnapshotEnvelope> storageToEnvelopeMapper,
        IMapper<SnapshotWriteModel, SnapshotBlobStorageModel> writeModelToStorageMapper,
        IOptions<SnapshotBlobStorageOptions> options,
        ILogger<SnapshotBlobRepository> logger
    )
    {
        ContainerOperations = containerOperations ?? throw new ArgumentNullException(nameof(containerOperations));
        StorageToEnvelopeMapper = storageToEnvelopeMapper ?? throw new ArgumentNullException(nameof(storageToEnvelopeMapper));
        WriteModelToStorageMapper =
            writeModelToStorageMapper ?? throw new ArgumentNullException(nameof(writeModelToStorageMapper));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ISnapshotBlobContainerOperations ContainerOperations { get; }

    private ILogger<SnapshotBlobRepository> Logger { get; }

    private SnapshotBlobStorageOptions Options { get; }

    private IMapper<SnapshotBlobStorageModel, SnapshotEnvelope> StorageToEnvelopeMapper { get; }

    private IMapper<SnapshotWriteModel, SnapshotBlobStorageModel> WriteModelToStorageMapper { get; }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        string prefix = SnapshotBlobPathBuilder.BuildStreamPrefix(streamKey);
        Logger.DeletingAllSnapshots(prefix);
        int count = 0;
        await foreach (SnapshotBlobListItem blob in ContainerOperations.ListBlobsAsync(prefix, cancellationToken))
        {
            await ContainerOperations.DeleteBlobIfExistsAsync(blob.Name, cancellationToken).ConfigureAwait(false);
            count++;
        }

        Logger.DeletedAllSnapshots(prefix, count);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.DeleteBlobIfExistsAsync(
            SnapshotBlobPathBuilder.BuildBlobName(snapshotKey),
            cancellationToken);

    /// <inheritdoc />
    public async Task<int> PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        string prefix = SnapshotBlobPathBuilder.BuildStreamPrefix(streamKey);
        Logger.PruningSnapshots(prefix, retainModuli.Count);
        List<(string BlobName, long Version)> blobs = [];
        List<string> blobNames = [];
        await foreach (SnapshotBlobListItem blob in ContainerOperations.ListBlobsAsync(prefix, cancellationToken))
        {
            blobNames.Add(blob.Name);
        }

        blobs.AddRange(ParseValidBlobs(streamKey, blobNames));

        if (blobs.Count == 0)
        {
            Logger.NoSnapshotsToPrune(prefix);
            return 0;
        }

        long maxVersion = blobs.Max(item => item.Version);
        HashSet<long> retainVersions = new(
            blobs.Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                .Select(item => item.Version));
        retainVersions.Add(maxVersion);
        int deletedCount = 0;
        foreach ((string blobName, long version) in blobs)
        {
            if (retainVersions.Contains(version))
            {
                continue;
            }

            await ContainerOperations.DeleteBlobIfExistsAsync(blobName, cancellationToken).ConfigureAwait(false);
            deletedCount++;
        }

        Logger.PrunedSnapshots(prefix, deletedCount, retainVersions.Count, maxVersion);
        return deletedCount;

        static IEnumerable<(string BlobName, long Version)> ParseValidBlobs(
            SnapshotStreamKey key,
            IEnumerable<string> names
        )
        {
            foreach (string name in names)
            {
                if (SnapshotBlobPathBuilder.TryParseVersion(key, name, out long version))
                {
                    yield return (name, version);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        string blobName = SnapshotBlobPathBuilder.BuildBlobName(snapshotKey);
        SnapshotBlobDownloadResult? download = await ContainerOperations.DownloadBlobAsync(blobName, cancellationToken)
            .ConfigureAwait(false);
        if (download is null)
        {
            return null;
        }

        byte[] data = download.IsCompressed
            ? SnapshotCompression.Decompress(download.Data)
            : download.Data;
        SnapshotBlobStorageModel storageModel = new(
            snapshotKey.Stream,
            snapshotKey.Version,
            data,
            download.DataContentType,
            download.DataSizeBytes > 0 ? download.DataSizeBytes : data.LongLength);
        return StorageToEnvelopeMapper.Map(storageModel);
    }

    /// <inheritdoc />
    public Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        SnapshotBlobStorageModel storageModel = WriteModelToStorageMapper.Map(new(snapshotKey, snapshot));
        bool compressed = Options.CompressionEnabled;
        byte[] data = compressed
            ? SnapshotCompression.Compress(storageModel.Data)
            : storageModel.Data;
        SnapshotBlobWriteRequest request = new(
            data,
            storageModel.DataContentType,
            storageModel.DataSizeBytes,
            compressed);
        return ContainerOperations.UploadBlobAsync(
            SnapshotBlobPathBuilder.BuildBlobName(snapshotKey),
            request,
            cancellationToken);
    }
}
