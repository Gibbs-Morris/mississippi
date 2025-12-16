using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly ISnapshotContainerOperations containerOperations;

    private readonly IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper;

    private readonly IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper;

    private readonly IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper;

    private readonly IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCosmosRepository" /> class.
    /// </summary>
    /// <param name="containerOperations">The container operations abstraction for Cosmos access.</param>
    /// <param name="documentToStorageMapper">Maps Cosmos documents to snapshot storage models.</param>
    /// <param name="storageToEnvelopeMapper">Maps storage models to snapshot envelopes.</param>
    /// <param name="writeModelToStorageMapper">Maps snapshot write models to storage models.</param>
    /// <param name="storageToDocumentMapper">Maps storage models to Cosmos documents.</param>
    public SnapshotCosmosRepository(
        ISnapshotContainerOperations containerOperations,
        IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper,
        IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper
    )
    {
        this.containerOperations = containerOperations ?? throw new ArgumentNullException(nameof(containerOperations));
        this.documentToStorageMapper =
            documentToStorageMapper ?? throw new ArgumentNullException(nameof(documentToStorageMapper));
        this.storageToEnvelopeMapper =
            storageToEnvelopeMapper ?? throw new ArgumentNullException(nameof(storageToEnvelopeMapper));
        this.writeModelToStorageMapper = writeModelToStorageMapper ??
                                         throw new ArgumentNullException(nameof(writeModelToStorageMapper));
        this.storageToDocumentMapper =
            storageToDocumentMapper ?? throw new ArgumentNullException(nameof(storageToDocumentMapper));
    }

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
        await foreach (SnapshotIdVersion item in containerOperations.QuerySnapshotIdsAsync(
                           partitionKey,
                           cancellationToken))
        {
            await containerOperations.DeleteDocumentAsync(partitionKey, item.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        await containerOperations.DeleteDocumentAsync(
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
        List<SnapshotIdVersion> ids = new();
        await foreach (SnapshotIdVersion item in containerOperations.QuerySnapshotIdsAsync(
                           partitionKey,
                           cancellationToken))
        {
            ids.Add(item);
        }

        if (ids.Count == 0)
        {
            return;
        }

        long maxVersion = ids.Max(i => i.Version);
        HashSet<long> retainVersions = new(
            ids.Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                .Select(item => item.Version));
        retainVersions.Add(maxVersion);
        foreach (SnapshotIdVersion item in ids)
        {
            if (retainVersions.Contains(item.Version))
            {
                continue;
            }

            await containerOperations.DeleteDocumentAsync(partitionKey, item.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        SnapshotDocument? doc = await containerOperations.ReadDocumentAsync(
                ToPartitionKey(snapshotKey.Stream),
                ToDocumentId(snapshotKey.Version),
                cancellationToken)
            .ConfigureAwait(false);
        if (doc is null)
        {
            return null;
        }

        SnapshotStorageModel storage = documentToStorageMapper.Map(doc);
        return storageToEnvelopeMapper.Map(storage);
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        SnapshotWriteModel writeModel = new(snapshotKey, snapshot);
        SnapshotStorageModel storageModel = writeModelToStorageMapper.Map(writeModel);
        SnapshotDocument document = storageToDocumentMapper.Map(storageModel);
        await containerOperations.UpsertDocumentAsync(ToPartitionKey(snapshotKey.Stream), document, cancellationToken)
            .ConfigureAwait(false);
    }
}