using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos-backed implementation of <see cref="ISnapshotQueryService" />.
/// </summary>
internal sealed class CosmosSnapshotQueryService : ISnapshotQueryService
{
    private readonly Container container;

    private readonly SnapshotStorageOptions options;

    private readonly IRetryPolicy retryPolicy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosSnapshotQueryService" /> class.
    /// </summary>
    /// <param name="container">The Cosmos container used for snapshots.</param>
    /// <param name="options">The snapshot storage options.</param>
    /// <param name="retryPolicy">Retry policy for transient Cosmos errors.</param>
    public CosmosSnapshotQueryService(
        Container container,
        IOptions<SnapshotStorageOptions> options,
        IRetryPolicy retryPolicy
    )
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
        this.retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SnapshotIdVersion> ReadIdsAsync(
        SnapshotStreamKey streamKey,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
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
                PartitionKey = new(streamKey.ToString()),
                MaxItemCount = options.QueryBatchSize,
            });
        while (iterator.HasMoreResults)
        {
            FeedResponse<SnapshotIdVersionDto> page = await retryPolicy.ExecuteAsync(
                () => iterator.ReadNextAsync(cancellationToken),
                cancellationToken);
            foreach (SnapshotIdVersionDto item in page)
            {
                yield return new(item.Id, item.Version);
            }
        }
    }

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