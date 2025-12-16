using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos-backed implementation of <see cref="ISnapshotCosmosRepository" />.
/// </summary>
internal sealed class SnapshotCosmosRepository : ISnapshotCosmosRepository
{
    private readonly Container container;

    private readonly IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper;

    private readonly ISnapshotQueryService queryService;

    private readonly IRetryPolicy retryPolicy;

    private readonly IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper;

    private readonly IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper;

    private readonly IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCosmosRepository" /> class.
    /// </summary>
    /// <param name="container">The Cosmos container used for snapshots.</param>
    /// <param name="queryService">The query service for reading snapshot identifiers.</param>
    /// <param name="documentToStorageMapper">Maps Cosmos documents to snapshot storage models.</param>
    /// <param name="storageToEnvelopeMapper">Maps storage models to snapshot envelopes.</param>
    /// <param name="writeModelToStorageMapper">Maps snapshot write models to storage models.</param>
    /// <param name="storageToDocumentMapper">Maps storage models to Cosmos documents.</param>
    /// <param name="retryPolicy">Retry policy for transient Cosmos errors.</param>
    public SnapshotCosmosRepository(
        Container container,
        ISnapshotQueryService queryService,
        IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper,
        IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper,
        IRetryPolicy retryPolicy
    )
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        this.documentToStorageMapper =
            documentToStorageMapper ?? throw new ArgumentNullException(nameof(documentToStorageMapper));
        this.storageToEnvelopeMapper =
            storageToEnvelopeMapper ?? throw new ArgumentNullException(nameof(storageToEnvelopeMapper));
        this.writeModelToStorageMapper = writeModelToStorageMapper ??
                                         throw new ArgumentNullException(nameof(writeModelToStorageMapper));
        this.storageToDocumentMapper =
            storageToDocumentMapper ?? throw new ArgumentNullException(nameof(storageToDocumentMapper));
        this.retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
    }

    private static PartitionKey Partition(
        SnapshotStreamKey streamKey
    ) =>
        new(streamKey.ToString());

    private static string ToDocumentId(
        long version
    ) =>
        version.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        await foreach (SnapshotIdVersion item in queryService.ReadIdsAsync(streamKey, cancellationToken))
        {
            await DeleteDocumentAsync(streamKey, item.Id, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await retryPolicy.ExecuteAsync(
                () => container.DeleteItemAsync<SnapshotDocument>(
                    ToDocumentId(snapshotKey.Version),
                    Partition(snapshotKey.Stream),
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already deleted.
        }
    }

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        List<SnapshotIdVersion> ids = new();
        await foreach (SnapshotIdVersion item in queryService.ReadIdsAsync(streamKey, cancellationToken))
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

            await DeleteDocumentAsync(streamKey, item.Id, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ItemResponse<SnapshotDocument> response = await retryPolicy.ExecuteAsync(
                () => container.ReadItemAsync<SnapshotDocument>(
                    ToDocumentId(snapshotKey.Version),
                    Partition(snapshotKey.Stream),
                    cancellationToken: cancellationToken),
                cancellationToken);
            SnapshotDocument doc = response.Resource;
            SnapshotStorageModel storage = documentToStorageMapper.Map(doc);
            return storageToEnvelopeMapper.Map(storage);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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
        await retryPolicy.ExecuteAsync(
            () => container.UpsertItemAsync(
                document,
                Partition(snapshotKey.Stream),
                cancellationToken: cancellationToken),
            cancellationToken);
    }

    private async Task DeleteDocumentAsync(
        SnapshotStreamKey streamKey,
        string id,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await retryPolicy.ExecuteAsync(
                () => container.DeleteItemAsync<SnapshotDocument>(
                    id,
                    Partition(streamKey),
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already removed.
        }
    }
}