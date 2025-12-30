using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos-backed implementation of <see cref="ISnapshotCosmosRepository" />.
/// </summary>
/// <remarks>
///     <para>
///         This class follows the Single Responsibility Principle (SRP) by focusing solely
///         on domain-level snapshot operations, delegating all Cosmos SDK interactions
///         to <see cref="ISnapshotContainerOperations" />.
///     </para>
///     <para>
///         Dependency Inversion Principle (DIP): Depends on abstractions (ISnapshotContainerOperations,
///         IMapper) rather than concrete implementations or the Cosmos SDK directly.
///     </para>
/// </remarks>
internal sealed class SnapshotCosmosRepository : ISnapshotCosmosRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCosmosRepository" /> class.
    /// </summary>
    /// <param name="containerOperations">The container operations abstraction for Cosmos access.</param>
    /// <param name="documentToStorageMapper">Maps Cosmos documents to snapshot storage models.</param>
    /// <param name="storageToEnvelopeMapper">Maps storage models to snapshot envelopes.</param>
    /// <param name="writeModelToStorageMapper">Maps snapshot write models to storage models.</param>
    /// <param name="storageToDocumentMapper">Maps storage models to Cosmos documents.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotCosmosRepository(
        ISnapshotContainerOperations containerOperations,
        IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper,
        IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper,
        ILogger<SnapshotCosmosRepository> logger
    )
    {
        ContainerOperations = containerOperations ?? throw new ArgumentNullException(nameof(containerOperations));
        DocumentToStorageMapper =
            documentToStorageMapper ?? throw new ArgumentNullException(nameof(documentToStorageMapper));
        StorageToEnvelopeMapper =
            storageToEnvelopeMapper ?? throw new ArgumentNullException(nameof(storageToEnvelopeMapper));
        WriteModelToStorageMapper = writeModelToStorageMapper ??
                                    throw new ArgumentNullException(nameof(writeModelToStorageMapper));
        StorageToDocumentMapper =
            storageToDocumentMapper ?? throw new ArgumentNullException(nameof(storageToDocumentMapper));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ISnapshotContainerOperations ContainerOperations { get; }

    private IMapper<SnapshotDocument, SnapshotStorageModel> DocumentToStorageMapper { get; }

    private ILogger<SnapshotCosmosRepository> Logger { get; }

    private IMapper<SnapshotStorageModel, SnapshotDocument> StorageToDocumentMapper { get; }

    private IMapper<SnapshotStorageModel, SnapshotEnvelope> StorageToEnvelopeMapper { get; }

    private IMapper<SnapshotWriteModel, SnapshotStorageModel> WriteModelToStorageMapper { get; }

    private static string ToDocumentId(
        long version
    ) =>
        version.ToString(CultureInfo.InvariantCulture);

    private static string ToPartitionKey(
        SnapshotStreamKey streamKey
    ) =>
        streamKey.ToString();

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        string partitionKey = ToPartitionKey(streamKey);
        Logger.DeletingAllSnapshots(partitionKey);
        int count = 0;
        await foreach (SnapshotIdVersion item in ContainerOperations.QuerySnapshotIdsAsync(
                           partitionKey,
                           cancellationToken))
        {
            await ContainerOperations.DeleteDocumentAsync(partitionKey, item.Id, cancellationToken)
                .ConfigureAwait(false);
            count++;
        }

        Logger.DeletedAllSnapshots(partitionKey, count);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        await ContainerOperations.DeleteDocumentAsync(
                ToPartitionKey(snapshotKey.Stream),
                ToDocumentId(snapshotKey.Version),
                cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        string partitionKey = ToPartitionKey(streamKey);
        Logger.PruningSnapshots(partitionKey, retainModuli.Count);
        List<SnapshotIdVersion> ids = new();
        await foreach (SnapshotIdVersion item in ContainerOperations.QuerySnapshotIdsAsync(
                           partitionKey,
                           cancellationToken))
        {
            ids.Add(item);
        }

        if (ids.Count == 0)
        {
            Logger.NoSnapshotsToPrune(partitionKey);
            return;
        }

        long maxVersion = ids.Max(i => i.Version);
        HashSet<long> retainVersions = new(
            ids.Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                .Select(item => item.Version));
        retainVersions.Add(maxVersion);
        int deletedCount = 0;
        foreach (SnapshotIdVersion item in ids)
        {
            if (retainVersions.Contains(item.Version))
            {
                continue;
            }

            await ContainerOperations.DeleteDocumentAsync(partitionKey, item.Id, cancellationToken)
                .ConfigureAwait(false);
            deletedCount++;
        }

        Logger.PrunedSnapshots(partitionKey, deletedCount, retainVersions.Count, maxVersion);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        SnapshotDocument? doc = await ContainerOperations.ReadDocumentAsync(
                ToPartitionKey(snapshotKey.Stream),
                ToDocumentId(snapshotKey.Version),
                cancellationToken)
            .ConfigureAwait(false);
        if (doc is null)
        {
            return null;
        }

        SnapshotStorageModel storage = DocumentToStorageMapper.Map(doc);
        return StorageToEnvelopeMapper.Map(storage);
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        SnapshotWriteModel writeModel = new(snapshotKey, snapshot);
        SnapshotStorageModel storageModel = WriteModelToStorageMapper.Map(writeModel);
        SnapshotDocument document = StorageToDocumentMapper.Map(storageModel);
        await ContainerOperations.UpsertDocumentAsync(ToPartitionKey(snapshotKey.Stream), document, cancellationToken)
            .ConfigureAwait(false);
    }
}