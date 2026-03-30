using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Encapsulates Cosmos SDK interactions for a single named replica sink registration.
/// </summary>
internal sealed class CosmosReplicaSinkContainerOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkContainerOperations" /> class.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="options">The configured sink options.</param>
    /// <param name="cosmosClient">The keyed Cosmos client backing the sink.</param>
    /// <param name="container">The keyed Cosmos container backing the sink.</param>
    /// <param name="retryPolicy">The retry policy for transient Cosmos operations.</param>
    /// <param name="timeProvider">The time provider for deterministic timestamps.</param>
    /// <param name="logger">The logger for Cosmos operation diagnostics.</param>
    public CosmosReplicaSinkContainerOperations(
        string sinkKey,
        CosmosReplicaSinkOptions options,
        CosmosClient cosmosClient,
        Container container,
        IRetryPolicy retryPolicy,
        TimeProvider timeProvider,
        ILogger<CosmosReplicaSinkContainerOperations> logger
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        SinkKey = sinkKey;
        Options = options;
        CosmosClient = cosmosClient;
        CosmosContainer = container;
        RetryPolicy = retryPolicy;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private Container CosmosContainer { get; }

    private CosmosClient CosmosClient { get; }

    private ILogger<CosmosReplicaSinkContainerOperations> Logger { get; }

    private CosmosReplicaSinkOptions Options { get; }

    private IRetryPolicy RetryPolicy { get; }

    private string SinkKey { get; }

    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Gets the current UTC timestamp from the configured time provider.
    /// </summary>
    /// <returns>The current UTC timestamp.</returns>
    public DateTimeOffset GetCurrentUtcNow() => TimeProvider.GetUtcNow();

    /// <summary>
    ///     Ensures the configured database/container exists or is valid according to the supplied provisioning mode.
    /// </summary>
    /// <param name="provisioningMode">The provisioning mode to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EnsureContainerAsync(
        ReplicaProvisioningMode provisioningMode,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogEnsuringContainer(SinkKey, Options.DatabaseId, Options.ContainerId, provisioningMode.ToString());
        if (provisioningMode == ReplicaProvisioningMode.CreateIfMissing)
        {
            await RetryPolicy.ExecuteAsync(
                    () => CosmosClient.CreateDatabaseIfNotExistsAsync(Options.DatabaseId, cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);

            Database database = CosmosClient.GetDatabase(Options.DatabaseId);
            ContainerResponse response = await RetryPolicy.ExecuteAsync(
                    () => database.CreateContainerIfNotExistsAsync(
                        new ContainerProperties(Options.ContainerId, ReplicaSinkCosmosDefaults.PartitionKeyPath),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            ValidatePartitionKey(response.Resource?.PartitionKeyPath);
            Logger.LogContainerProvisioned(SinkKey, Options.DatabaseId, Options.ContainerId);
            return;
        }

        try
        {
            ContainerResponse response = await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.ReadContainerAsync(cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            ValidatePartitionKey(response.Resource?.PartitionKeyPath);
            Logger.LogContainerValidated(SinkKey, Options.DatabaseId, Options.ContainerId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.LogContainerMissing(
                ex,
                SinkKey,
                Options.DatabaseId,
                Options.ContainerId,
                provisioningMode.ToString());
            throw new InvalidOperationException(
                $"Cosmos replica sink '{SinkKey}' requires existing container '{Options.DatabaseId}/{Options.ContainerId}' when provisioning mode '{provisioningMode}' is used.",
                ex);
        }
    }

    /// <summary>
    ///     Creates the target marker document when the target does not already exist.
    /// </summary>
    /// <param name="targetName">The provider-facing target name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateTargetAsync(
        string targetName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        Logger.LogCreatingTarget(SinkKey, targetName);
        CosmosReplicaSinkTargetMarkerDocument document = CosmosReplicaSinkTargetMarkerDocument.Create(
            targetName,
            TimeProvider.GetUtcNow());
        try
        {
            await ExecuteAsync(
                    () => CosmosContainer.CreateItemAsync(
                        document,
                        new PartitionKey(targetName),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            Logger.LogTargetCreated(SinkKey, targetName);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            Logger.LogTargetAlreadyExists(ex, SinkKey, targetName);
        }
    }

    /// <summary>
    ///     Inspects the current target summary for the supplied target name.
    /// </summary>
    /// <param name="targetName">The provider-facing target name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current target inspection snapshot.</returns>
    public async Task<CosmosReplicaSinkTargetInspectionSnapshot> InspectTargetAsync(
        string targetName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        Logger.LogInspectingTarget(SinkKey, targetName);
        CosmosReplicaSinkTargetMarkerDocument? marker = await ReadTargetMarkerAsync(targetName, cancellationToken)
            .ConfigureAwait(false);
        if (marker is null)
        {
            Logger.LogTargetInspected(SinkKey, targetName, false, 0);
            return new(false, 0);
        }

        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type")
            .WithParameter("@type", CosmosReplicaSinkDocumentKeys.TargetDeliveryDocumentType);
        IReadOnlyList<CosmosReplicaSinkTargetDeliveryDocument> deliveries = await QueryDocumentsAsync<CosmosReplicaSinkTargetDeliveryDocument>(
                query,
                targetName,
                null,
                cancellationToken)
            .ConfigureAwait(false);

        long writeCount = 0;
        CosmosReplicaSinkTargetDeliveryDocument? latest = null;
        foreach (CosmosReplicaSinkTargetDeliveryDocument delivery in deliveries)
        {
            writeCount += delivery.AppliedWriteCount;
            if (latest is null || IsNewer(delivery, latest))
            {
                latest = delivery;
            }
        }

        Logger.LogTargetInspected(SinkKey, targetName, true, writeCount);
        return new(
            true,
            writeCount,
            latest?.LatestSourcePosition,
            latest?.GetLatestPayload());
    }

    /// <summary>
    ///     Reads the current target-delivery document for the supplied lane.
    /// </summary>
    /// <param name="targetName">The provider-facing target name.</param>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current target-delivery document, or <see langword="null" /> when none exists.</returns>
    public async Task<CosmosReplicaSinkTargetDeliveryDocument?> ReadTargetDeliveryAsync(
        string targetName,
        string deliveryKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        try
        {
            ItemResponse<CosmosReplicaSinkTargetDeliveryDocument> response = await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.ReadItemAsync<CosmosReplicaSinkTargetDeliveryDocument>(
                        CosmosReplicaSinkDocumentKeys.TargetDeliveryId(deliveryKey),
                        new PartitionKey(targetName),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.LogTargetDeliveryNotFound(ex, SinkKey, targetName, deliveryKey);
            return null;
        }
    }

    /// <summary>
    ///     Reads the durable delivery-state snapshot for the supplied delivery key.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The durable delivery-state snapshot, or <see langword="null" /> when none exists.</returns>
    public async Task<ReplicaSinkDeliveryState?> ReadDeliveryStateAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        try
        {
            ItemResponse<CosmosReplicaSinkDeliveryStateDocument> response = await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.ReadItemAsync<CosmosReplicaSinkDeliveryStateDocument>(
                        CosmosReplicaSinkDocumentKeys.DeliveryStateId(deliveryKey),
                        new PartitionKey(ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            return response.Resource.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.LogDeliveryStateNotFound(ex, SinkKey, deliveryKey);
            return null;
        }
    }

    /// <summary>
    ///     Reads the currently due retry states from the durable delivery-state partition.
    /// </summary>
    /// <param name="dueAtOrBeforeUtc">The inclusive due-time cutoff.</param>
    /// <param name="maxCount">The maximum number of states to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The currently due retry states.</returns>
    public async Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCount);
        if (maxCount == 0)
        {
            return Array.Empty<ReplicaSinkDeliveryState>();
        }

        string dueAt = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(dueAtOrBeforeUtc);
        QueryDefinition query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type AND IS_DEFINED(c.retry.nextRetryAtUtc) AND c.retry.nextRetryAtUtc != null AND c.retry.nextRetryAtUtc <= @dueAt ORDER BY c.retry.nextRetryAtUtc ASC, c.deliveryKey ASC")
            .WithParameter("@type", CosmosReplicaSinkDocumentKeys.DeliveryStateDocumentType)
            .WithParameter("@dueAt", dueAt);
        IReadOnlyList<CosmosReplicaSinkDeliveryStateDocument> documents = await QueryDocumentsAsync<CosmosReplicaSinkDeliveryStateDocument>(
                query,
                ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey,
                maxCount,
                cancellationToken)
            .ConfigureAwait(false);
        Logger.LogQueryCompleted(SinkKey, "due retries", documents.Count);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    /// <summary>
    ///     Reads the currently persisted dead-letter states from the durable delivery-state partition.
    /// </summary>
    /// <param name="maxCount">The maximum number of states to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The currently persisted dead-letter states.</returns>
    public async Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDeadLettersAsync(
        int maxCount,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCount);
        if (maxCount == 0)
        {
            return Array.Empty<ReplicaSinkDeliveryState>();
        }

        QueryDefinition query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type AND IS_DEFINED(c.deadLetter.recordedAtUtc) AND c.deadLetter.recordedAtUtc != null ORDER BY c.deadLetter.recordedAtUtc DESC, c.deliveryKey ASC")
            .WithParameter("@type", CosmosReplicaSinkDocumentKeys.DeliveryStateDocumentType);
        IReadOnlyList<CosmosReplicaSinkDeliveryStateDocument> documents = await QueryDocumentsAsync<CosmosReplicaSinkDeliveryStateDocument>(
                query,
                ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey,
                maxCount,
                cancellationToken)
            .ConfigureAwait(false);
        Logger.LogQueryCompleted(SinkKey, "dead letters", documents.Count);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    /// <summary>
    ///     Determines whether the specified target marker already exists.
    /// </summary>
    /// <param name="targetName">The provider-facing target name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true" /> when the marker exists; otherwise <see langword="false" />.</returns>
    public async Task<bool> TargetExistsAsync(
        string targetName,
        CancellationToken cancellationToken = default
    ) => await ReadTargetMarkerAsync(targetName, cancellationToken).ConfigureAwait(false) is not null;

    /// <summary>
    ///     Upserts the target-delivery document for the supplied lane.
    /// </summary>
    /// <param name="document">The target-delivery document.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpsertTargetDeliveryAsync(
        CosmosReplicaSinkTargetDeliveryDocument document,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        Logger.LogWritingTargetDelivery(SinkKey, document.TargetName, document.DeliveryKey, document.LatestSourcePosition);
        await ExecuteAsync(
                () => CosmosContainer.UpsertItemAsync(
                    document,
                    new PartitionKey(document.TargetName),
                    cancellationToken: cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
        Logger.LogTargetDeliveryWritten(SinkKey, document.TargetName, document.DeliveryKey, document.LatestSourcePosition);
    }

    /// <summary>
    ///     Upserts the durable delivery-state document for the supplied state snapshot.
    /// </summary>
    /// <param name="state">The durable delivery-state snapshot.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WriteDeliveryStateAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        CosmosReplicaSinkDeliveryStateDocument document = CosmosReplicaSinkDeliveryStateDocument.FromDomain(state);
        Logger.LogWritingDeliveryState(SinkKey, state.DeliveryKey);
        await ExecuteAsync(
                () => CosmosContainer.UpsertItemAsync(
                    document,
                    new PartitionKey(ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey),
                    cancellationToken: cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
        Logger.LogDeliveryStateWritten(SinkKey, state.DeliveryKey);
    }

    private static bool IsNewer(
        CosmosReplicaSinkTargetDeliveryDocument candidate,
        CosmosReplicaSinkTargetDeliveryDocument current
    )
    {
        if (candidate.LatestSourcePosition != current.LatestSourcePosition)
        {
            return candidate.LatestSourcePosition > current.LatestSourcePosition;
        }

        int updatedComparison = StringComparer.Ordinal.Compare(candidate.LastUpdatedAtUtc, current.LastUpdatedAtUtc);
        if (updatedComparison != 0)
        {
            return updatedComparison > 0;
        }

        return StringComparer.Ordinal.Compare(candidate.DeliveryKey, current.DeliveryKey) < 0;
    }

    private async Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken
    )
    {
        await RetryPolicy.ExecuteAsync(
                async () =>
                {
                    await operation().ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TDocument>> QueryDocumentsAsync<TDocument>(
        QueryDefinition query,
        string partitionKey,
        int? maxCount,
        CancellationToken cancellationToken
    )
    {
        if (maxCount == 0)
        {
            return Array.Empty<TDocument>();
        }

        List<TDocument> documents = [];
        using FeedIterator<TDocument> iterator = CosmosContainer.GetItemQueryIterator<TDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey),
                MaxItemCount = Options.QueryBatchSize,
            });
        while (iterator.HasMoreResults && (!maxCount.HasValue || documents.Count < maxCount.Value))
        {
            FeedResponse<TDocument> page = await RetryPolicy.ExecuteAsync(
                    () => iterator.ReadNextAsync(cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (TDocument document in page)
            {
                documents.Add(document);
                if (maxCount.HasValue && documents.Count >= maxCount.Value)
                {
                    break;
                }
            }
        }

        return documents;
    }

    private async Task<CosmosReplicaSinkTargetMarkerDocument?> ReadTargetMarkerAsync(
        string targetName,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ItemResponse<CosmosReplicaSinkTargetMarkerDocument> response = await RetryPolicy.ExecuteAsync(
                    () => CosmosContainer.ReadItemAsync<CosmosReplicaSinkTargetMarkerDocument>(
                        CosmosReplicaSinkDocumentKeys.TargetMarkerId,
                        new PartitionKey(targetName),
                        cancellationToken: cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.LogTargetMarkerNotFound(ex, SinkKey, targetName);
            return null;
        }
    }

    private static void ValidatePartitionKey(
        string? partitionKeyPath
    )
    {
        if (!string.Equals(partitionKeyPath, ReplicaSinkCosmosDefaults.PartitionKeyPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cosmos replica sink containers must use partition key path '{ReplicaSinkCosmosDefaults.PartitionKeyPath}', but '{partitionKeyPath ?? "<null>"}' was found.");
        }
    }
}
