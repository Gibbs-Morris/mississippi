using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos-backed implementation of <see cref="ISnapshotCosmosRepository" />.
/// </summary>
internal sealed class SnapshotCosmosRepository : ISnapshotCosmosRepository
{
    private readonly Container container;

    private readonly IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper;

    private readonly SnapshotStorageOptions options;

    private readonly IRetryPolicy retryPolicy;

    private readonly IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper;

    private readonly IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper;

    private readonly IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCosmosRepository" /> class.
    /// </summary>
    /// <param name="container">The Cosmos container used for snapshots.</param>
    /// <param name="options">The snapshot storage options.</param>
    /// <param name="documentToStorageMapper">Maps Cosmos documents to snapshot storage models.</param>
    /// <param name="storageToEnvelopeMapper">Maps storage models to snapshot envelopes.</param>
    /// <param name="writeModelToStorageMapper">Maps snapshot write models to storage models.</param>
    /// <param name="storageToDocumentMapper">Maps storage models to Cosmos documents.</param>
    /// <param name="retryPolicy">Retry policy for transient Cosmos errors.</param>
    public SnapshotCosmosRepository(
        Container container,
        IOptions<SnapshotStorageOptions> options,
        IMapper<SnapshotDocument, SnapshotStorageModel> documentToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotEnvelope> storageToEnvelopeMapper,
        IMapper<SnapshotWriteModel, SnapshotStorageModel> writeModelToStorageMapper,
        IMapper<SnapshotStorageModel, SnapshotDocument> storageToDocumentMapper,
        IRetryPolicy retryPolicy
    )
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
        this.documentToStorageMapper =
            documentToStorageMapper ?? throw new ArgumentNullException(nameof(documentToStorageMapper));
        this.storageToEnvelopeMapper =
            storageToEnvelopeMapper ?? throw new ArgumentNullException(nameof(storageToEnvelopeMapper));
        this.writeModelToStorageMapper = writeModelToStorageMapper ??
                                         throw new ArgumentNullException(nameof(writeModelToStorageMapper));
        this.storageToDocumentMapper =
            storageToDocumentMapper ?? throw new ArgumentNullException(nameof(storageToDocumentMapper));
        this.retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
        IReadOnlyList<SnapshotIdVersion> ids = await ReadIdsAsync(streamKey, cancellationToken);
        foreach (SnapshotIdVersion item in ids)
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
        IReadOnlyList<SnapshotIdVersion> ids = await ReadIdsAsync(streamKey, cancellationToken);
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

    private async Task<IReadOnlyList<SnapshotIdVersion>> ReadIdsAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken
    )
    {
        QueryDefinition query =
            new QueryDefinition("SELECT c.id, c.version FROM c WHERE c.snapshotPartitionKey = @pk").WithParameter(
                "@pk",
                streamKey.ToString());
        using FeedIterator<SnapshotIdVersionDto> iterator = container.GetItemQueryIterator<SnapshotIdVersionDto>(
            query,
            requestOptions: new()
            {
                PartitionKey = Partition(streamKey),
                MaxItemCount = options.QueryBatchSize,
            });
        List<SnapshotIdVersion> results = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<SnapshotIdVersionDto> page = await retryPolicy.ExecuteAsync(
                () => iterator.ReadNextAsync(cancellationToken),
                cancellationToken);
            results.AddRange(page.Select(item => new SnapshotIdVersion(item.Id, item.Version)));
        }

        return results;
    }

    private sealed record SnapshotIdVersion(string Id, long Version);

    private sealed class SnapshotIdVersionDto
    {
        [JsonConstructor]
        public SnapshotIdVersionDto(
            string id,
            long version
        )
        {
            Id = id ?? string.Empty;
            Version = version;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }

        [JsonProperty(PropertyName = "version")]
        public long Version { get; }
    }
}