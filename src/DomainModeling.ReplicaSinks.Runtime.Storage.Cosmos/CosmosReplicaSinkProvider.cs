using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Cosmos-backed implementation of the replica sink provider contract.
/// </summary>
internal sealed class CosmosReplicaSinkProvider
    : IReplicaSinkProvider,
      ICosmosReplicaSinkShard
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkProvider" /> class.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="options">The configured provider options.</param>
    /// <param name="containerOperations">The Cosmos storage helper for this sink registration.</param>
    /// <param name="logger">The provider logger.</param>
    public CosmosReplicaSinkProvider(
        string sinkKey,
        CosmosReplicaSinkOptions options,
        CosmosReplicaSinkContainerOperations containerOperations,
        ILogger<CosmosReplicaSinkProvider> logger
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(containerOperations);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        SinkKey = sinkKey;
        Options = options;
        ContainerOperations = containerOperations;
        Logger = logger;
    }

    /// <inheritdoc />
    public string Format => ReplicaSinkCosmosDefaults.FormatName;

    /// <inheritdoc />
    public string SinkKey { get; }

    private CosmosReplicaSinkContainerOperations ContainerOperations { get; }

    private ILogger<CosmosReplicaSinkProvider> Logger { get; }

    private CosmosReplicaSinkOptions Options { get; }

    /// <inheritdoc />
    public Task EnsureContainerAsync(
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.EnsureContainerAsync(Options.ProvisioningMode, cancellationToken);

    /// <inheritdoc />
    public async ValueTask EnsureTargetAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(target.DestinationIdentity);
        Logger.LogEnsuringTarget(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName,
            target.ProvisioningMode.ToString());
        await ContainerOperations.EnsureContainerAsync(target.ProvisioningMode, cancellationToken)
            .ConfigureAwait(false);
        if (await ContainerOperations.TargetExistsAsync(target.DestinationIdentity.TargetName, cancellationToken)
                .ConfigureAwait(false))
        {
            Logger.LogTargetValidated(
                SinkKey,
                target.DestinationIdentity.ClientKey,
                target.DestinationIdentity.TargetName);
            return;
        }

        if (target.ProvisioningMode != ReplicaProvisioningMode.CreateIfMissing)
        {
            throw new InvalidOperationException(
                $"Cosmos replica sink '{SinkKey}' target '{target.DestinationIdentity}' does not exist and provisioning mode '{target.ProvisioningMode}' does not allow creation.");
        }

        await ContainerOperations.CreateTargetAsync(target.DestinationIdentity.TargetName, cancellationToken)
            .ConfigureAwait(false);
        Logger.LogTargetProvisioned(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName);
    }

    /// <inheritdoc />
    public async ValueTask<ReplicaTargetInspection> InspectAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(target.DestinationIdentity);
        Logger.LogInspectingTarget(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName);
        CosmosReplicaSinkTargetInspectionSnapshot inspection = await ContainerOperations.InspectTargetAsync(
                target.DestinationIdentity.TargetName,
                cancellationToken)
            .ConfigureAwait(false);
        Logger.LogTargetInspected(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName,
            inspection.TargetExists,
            inspection.WriteCount);
        return new(
            target.DestinationIdentity,
            inspection.TargetExists,
            inspection.WriteCount,
            inspection.LatestSourcePosition,
            inspection.LatestPayload);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDeadLettersAsync(
        int maxCount,
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.ReadDeadLettersAsync(maxCount, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.ReadDueRetriesAsync(dueAtOrBeforeUtc, maxCount, cancellationToken);

    /// <inheritdoc />
    public Task<ReplicaSinkDeliveryState?> ReadStateAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.ReadDeliveryStateAsync(deliveryKey, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<ReplicaWriteResult> WriteAsync(
        ReplicaWriteRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(request.Target.DestinationIdentity);
        Logger.LogWritingReplica(
            SinkKey,
            request.Target.DestinationIdentity.ClientKey,
            request.Target.DestinationIdentity.TargetName,
            request.DeliveryKey,
            request.SourcePosition);
        if (!await ContainerOperations
                .TargetExistsAsync(request.Target.DestinationIdentity.TargetName, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Cosmos replica sink '{SinkKey}' target '{request.Target.DestinationIdentity}' has not been provisioned.");
        }

        CosmosReplicaSinkTargetDeliveryDocument? existing = await ContainerOperations.ReadTargetDeliveryAsync(
                request.Target.DestinationIdentity.TargetName,
                request.DeliveryKey,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (request.SourcePosition < existing.LatestSourcePosition)
            {
                Logger.LogReplicaSupersededIgnored(
                    SinkKey,
                    request.Target.DestinationIdentity.ClientKey,
                    request.Target.DestinationIdentity.TargetName,
                    request.SourcePosition);
                return new(
                    ReplicaWriteOutcome.SupersededIgnored,
                    request.Target.DestinationIdentity,
                    request.SourcePosition);
            }

            if (request.SourcePosition == existing.LatestSourcePosition)
            {
                Logger.LogReplicaDuplicateIgnored(
                    SinkKey,
                    request.Target.DestinationIdentity.ClientKey,
                    request.Target.DestinationIdentity.TargetName,
                    request.SourcePosition);
                return new(
                    ReplicaWriteOutcome.DuplicateIgnored,
                    request.Target.DestinationIdentity,
                    request.SourcePosition);
            }
        }

        CosmosReplicaSinkTargetDeliveryDocument document = CosmosReplicaSinkTargetDeliveryDocument.Create(
            request,
            existing?.AppliedWriteCount + 1 ?? 1,
            ContainerOperations.GetCurrentUtcNow());
        await ContainerOperations.UpsertTargetDeliveryAsync(document, cancellationToken).ConfigureAwait(false);
        Logger.LogReplicaApplied(
            SinkKey,
            request.Target.DestinationIdentity.ClientKey,
            request.Target.DestinationIdentity.TargetName,
            request.SourcePosition);
        return new(ReplicaWriteOutcome.Applied, request.Target.DestinationIdentity, request.SourcePosition);
    }

    /// <inheritdoc />
    public Task WriteStateAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    ) =>
        ContainerOperations.WriteDeliveryStateAsync(state, cancellationToken);

    private void ValidateClientKey(
        ReplicaDestinationIdentity destinationIdentity
    )
    {
        if (!string.Equals(destinationIdentity.ClientKey, Options.ClientKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cosmos replica sink '{SinkKey}' was registered for client key '{Options.ClientKey}', but '{destinationIdentity.ClientKey}' was requested.");
        }
    }
}