using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Cosmos.Abstractions.Retry;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos SDK implementation of <see cref="ISnapshotContainerOperations" />.
/// </summary>
/// <remarks>
///     <para>
///         This class is the single point of contact with the Cosmos SDK Container,
///         following the Dependency Inversion Principle (DIP). All Cosmos-specific
///         concerns (retry policies, partition keys, query options) are encapsulated here.
///     </para>
///     <para>
///         Single Responsibility Principle (SRP): This class is responsible only for
///         translating domain-level storage requests into Cosmos SDK operations.
///     </para>
/// </remarks>
internal sealed class SnapshotContainerOperations : ISnapshotContainerOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotContainerOperations" /> class.
    /// </summary>
    /// <param name="container">The Cosmos container for snapshot storage.</param>
    /// <param name="options">The snapshot storage options.</param>
    /// <param name="retryPolicy">Retry policy for transient Cosmos errors.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotContainerOperations(
        [FromKeyedServices(CosmosContainerKeys.Snapshots)]
        Container container,
        IOptions<SnapshotStorageOptions> options,
        IRetryPolicy retryPolicy,
        ILogger<SnapshotContainerOperations> logger
    )
    {
        CosmosContainer = container ?? throw new ArgumentNullException(nameof(container));
        RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private Container CosmosContainer { get; }

    private ILogger<SnapshotContainerOperations> Logger { get; }

    private SnapshotStorageOptions Options { get; }

    private IRetryPolicy RetryPolicy { get; }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(
        string partitionKey,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingDocument(partitionKey, documentId);
        try
        {
            await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.DeleteItemAsync<SnapshotDocument>(
                        documentId,
                        new(partitionKey),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            Logger.DocumentDeleted(partitionKey, documentId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.DocumentNotFoundForDeletion(partitionKey, documentId);
            return false;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SnapshotIdVersion> QuerySnapshotIdsAsync(
        string partitionKey,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Logger.QueryingSnapshotIds(partitionKey);
        QueryDefinition query =
            new QueryDefinition("SELECT c.id, c.version FROM c WHERE c.snapshotPartitionKey = @pk").WithParameter(
                "@pk",
                partitionKey);
        using FeedIterator<SnapshotIdVersionDto> iterator = CosmosContainer.GetItemQueryIterator<SnapshotIdVersionDto>(
            query,
            requestOptions: new()
            {
                PartitionKey = new PartitionKey(partitionKey),
                MaxItemCount = Options.QueryBatchSize,
            });
        while (iterator.HasMoreResults)
        {
            FeedResponse<SnapshotIdVersionDto> page = await RetryPolicy.ExecuteAsync(
                    () => iterator.ReadNextAsync(cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            Logger.SnapshotIdsPageRetrieved(partitionKey, page.Count);
            foreach (SnapshotIdVersionDto item in page)
            {
                yield return new(item.Id, item.Version);
            }
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotDocument?> ReadDocumentAsync(
        string partitionKey,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingDocument(partitionKey, documentId);
        try
        {
            ItemResponse<SnapshotDocument> response = await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.ReadItemAsync<SnapshotDocument>(
                        documentId,
                        new(partitionKey),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            Logger.DocumentFound(partitionKey, documentId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.DocumentNotFound(partitionKey, documentId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task UpsertDocumentAsync(
        string partitionKey,
        SnapshotDocument document,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        Logger.UpsertingDocument(partitionKey);
        await RetryPolicy.ExecuteAsync(
                () => CosmosContainer.UpsertItemAsync(
                    document,
                    new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
        Logger.DocumentUpserted(partitionKey);
    }

    /// <summary>
    ///     DTO for deserializing snapshot ID and version from Cosmos queries.
    /// </summary>
    private sealed class SnapshotIdVersionDto
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SnapshotIdVersionDto" /> class.
        /// </summary>
        /// <param name="id">The document identifier.</param>
        /// <param name="version">The snapshot version.</param>
        [JsonConstructor]
        public SnapshotIdVersionDto(
            string id,
            long version
        )
        {
            Id = id ?? string.Empty;
            Version = version;
        }

        /// <summary>
        ///     Gets the document identifier.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; }

        /// <summary>
        ///     Gets the snapshot version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public long Version { get; }
    }
}