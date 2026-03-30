using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Implements the durable latest-state core for replica sink delivery lanes.
/// </summary>
internal sealed class ReplicaSinkLatestStateProcessor : IReplicaSinkLatestStateProcessor
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkLatestStateProcessor" /> class.
    /// </summary>
    /// <param name="registry">The replica sink binding registry.</param>
    /// <param name="deliveryStateStore">The durable delivery-state store.</param>
    /// <param name="sourceStateAccessor">The source-state accessor.</param>
    /// <param name="timeProvider">The time provider used for retry/dead-letter timestamps.</param>
    /// <param name="hook">The post-write/pre-checkpoint hook.</param>
    /// <param name="healthManager">The sink execution health manager.</param>
    /// <param name="runtimeOptions">The runtime options.</param>
    /// <param name="logger">The logger.</param>
    public ReplicaSinkLatestStateProcessor(
        IReplicaSinkProjectionRegistry registry,
        IReplicaSinkDeliveryStateStore deliveryStateStore,
        IReplicaSinkSourceStateAccessor sourceStateAccessor,
        TimeProvider timeProvider,
        IReplicaSinkLatestStateProcessorHook hook,
        IReplicaSinkExecutionHealthManager healthManager,
        IOptions<ReplicaSinkRuntimeOptions> runtimeOptions,
        ILogger<ReplicaSinkLatestStateProcessor> logger
    )
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        DeliveryStateStore = deliveryStateStore ?? throw new ArgumentNullException(nameof(deliveryStateStore));
        SourceStateAccessor = sourceStateAccessor ?? throw new ArgumentNullException(nameof(sourceStateAccessor));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Hook = hook ?? throw new ArgumentNullException(nameof(hook));
        HealthManager = healthManager ?? throw new ArgumentNullException(nameof(healthManager));
        RuntimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IReplicaSinkExecutionHealthManager HealthManager { get; }

    private IReplicaSinkDeliveryStateStore DeliveryStateStore { get; }

    private IReplicaSinkLatestStateProcessorHook Hook { get; }

    private ILogger<ReplicaSinkLatestStateProcessor> Logger { get; }

    private IReplicaSinkProjectionRegistry Registry { get; }

    private IOptions<ReplicaSinkRuntimeOptions> RuntimeOptions { get; }

    private IReplicaSinkSourceStateAccessor SourceStateAccessor { get; }

    private TimeProvider TimeProvider { get; }

    private static ReplicaWriteRequest BuildWriteRequest(
        BindingWorkItem workItem,
        ReplicaSinkSourceState sourceState,
        long sourcePosition
    )
    {
        if (sourceState.Kind == ReplicaSinkSourceStateKind.Value)
        {
            object payload = workItem.Binding.Map(sourceState.Value!) ??
                             throw new InvalidOperationException("Replica sink mapping produced a null payload.");
            return new(
                workItem.Binding.Target,
                workItem.DeliveryIdentity.DeliveryKey,
                sourcePosition,
                workItem.Binding.WriteMode,
                workItem.Binding.ContractIdentity,
                payload);
        }

        return new(
            workItem.Binding.Target,
            workItem.DeliveryIdentity.DeliveryKey,
            sourcePosition,
            workItem.Binding.WriteMode,
            workItem.Binding.ContractIdentity,
            null)
        {
            IsDeleted = true,
        };
    }

    private static string CreateUnexpectedFailureSummary(
        string prefix,
        Exception exception
    )
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix} ({exception.GetType().Name}).";
    }

    private static int GetNextAttemptCount(
        ReplicaSinkDeliveryState state,
        long sourcePosition
    )
    {
        if (state.Retry is not null && (state.Retry.SourcePosition == sourcePosition))
        {
            return state.Retry.AttemptCount + 1;
        }

        if (state.DeadLetter is not null && (state.DeadLetter.SourcePosition == sourcePosition))
        {
            return state.DeadLetter.AttemptCount + 1;
        }

        return 1;
    }

    private static bool IsCriticalException(
        Exception exception
    ) =>
        exception is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;

    private static bool IsTerminalOutcome(
        ReplicaWriteOutcome outcome
    ) =>
        outcome is ReplicaWriteOutcome.Applied or ReplicaWriteOutcome.DuplicateIgnored
            or ReplicaWriteOutcome.SupersededIgnored;

    private static bool IsAuthenticationFailureCode(
        string failureCode
    ) =>
        failureCode.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
        failureCode.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
        failureCode.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
        failureCode.Contains("credential", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldFlush(
        ReplicaSinkDeliveryState state,
        long targetSourcePosition,
        DateTimeOffset now
    ) =>
        (state.CommittedSourcePosition is null || (state.CommittedSourcePosition.Value < targetSourcePosition)) &&
        (state.DeadLetter is null || (state.DeadLetter.SourcePosition != targetSourcePosition)) &&
        (state.Retry is null ||
         (state.Retry.SourcePosition != targetSourcePosition) ||
         state.Retry.NextRetryAtUtc is null ||
         state.Retry.NextRetryAtUtc.Value <= now);

    /// <inheritdoc />
    public Task AdvanceDesiredPositionAsync<TProjection>(
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class =>
        AdvanceDesiredPositionAsync(typeof(TProjection), entityId, desiredSourcePosition, cancellationToken);

    /// <inheritdoc />
    public async Task AdvanceDesiredPositionAsync(
        Type projectionType,
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentOutOfRangeException.ThrowIfNegative(desiredSourcePosition);
        ReplicaSinkBindingDescriptor[] bindings = GetBindings(projectionType);
        if (bindings.Length == 0)
        {
            return;
        }

        foreach (ReplicaSinkBindingDescriptor binding in bindings)
        {
            ReplicaSinkDeliveryIdentity deliveryIdentity = new(binding.Identity, entityId);
            ReplicaSinkDeliveryState currentState = await ReadStateAsync(deliveryIdentity, cancellationToken);
            ReplicaSinkDeliveryState updatedState = ReplicaSinkDeliveryStateTransitions.AdvanceDesiredPosition(
                currentState,
                desiredSourcePosition,
                deliveryIdentity);
            await DeliveryStateStore.WriteAsync(updatedState, cancellationToken);
        }

        Logger.DesiredWatermarkAdvanced(projectionType.Name, entityId, desiredSourcePosition);
    }

    /// <inheritdoc />
    public Task FlushAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where TProjection : class =>
        FlushAsync(typeof(TProjection), entityId, cancellationToken);

    /// <inheritdoc />
    public async Task FlushAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ReplicaSinkBindingDescriptor[] bindings = GetBindings(projectionType);
        if (bindings.Length == 0)
        {
            return;
        }

        List<BindingWorkItem> workItems = await LoadWorkItemsAsync(bindings, entityId, cancellationToken);
        long? highestDesiredSourcePosition = workItems
            .Where(static item => item.State.DesiredSourcePosition is not null)
            .Select(static item => item.State.DesiredSourcePosition)
            .Max();
        if (highestDesiredSourcePosition is null)
        {
            return;
        }

        long highestDesired = highestDesiredSourcePosition.Value;
        foreach (BindingWorkItem workItem in workItems)
        {
            if (workItem.State.DesiredSourcePosition is null ||
                (workItem.State.DesiredSourcePosition.Value < highestDesired))
            {
                workItem.State = ReplicaSinkDeliveryStateTransitions.AdvanceDesiredPosition(
                    workItem.State,
                    highestDesired,
                    workItem.DeliveryIdentity);
                await DeliveryStateStore.WriteAsync(workItem.State, cancellationToken);
            }
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        IOrderedEnumerable<IGrouping<long, BindingWorkItem>> pendingGroups = workItems
            .Where(item => !HealthManager.IsBlocked(item.Binding.Identity.SinkKey))
            .Select(item => new
            {
                Item = item,
                TargetSourcePosition = ReplicaSinkDeliveryStateTransitions.GetEffectiveTargetSourcePosition(item.State),
            })
            .Where(candidate => candidate.TargetSourcePosition is not null &&
                                ShouldFlush(candidate.Item.State, candidate.TargetSourcePosition.Value, now))
            .GroupBy(candidate => candidate.TargetSourcePosition!.Value, candidate => candidate.Item)
            .OrderBy(group => group.Key);
        foreach (IGrouping<long, BindingWorkItem> pendingGroup in pendingGroups)
        {
            long targetSourcePosition = pendingGroup.Key;
            List<BindingWorkItem> pendingWorkItems = pendingGroup.ToList();
            ReplicaSinkSourceState sourceState;
            try
            {
                sourceState = await SourceStateAccessor.ReadAsync(
                    projectionType,
                    entityId,
                    targetSourcePosition,
                    cancellationToken);
            }
            catch (ReplicaSinkSourceStateUnavailableException ex)
            {
                Logger.SourceStateUnavailable(projectionType.Name, entityId, targetSourcePosition, ex);
                foreach (BindingWorkItem workItem in pendingWorkItems)
                {
                    await PersistRetryAsync(
                        workItem,
                        targetSourcePosition,
                        "source_state_unavailable",
                        "Projection source state is not yet available at the requested source position.",
                        false,
                        cancellationToken);
                }

                continue;
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                Logger.SourceStateReadFailed(projectionType.Name, entityId, targetSourcePosition, ex);
                foreach (BindingWorkItem workItem in pendingWorkItems)
                {
                    await PersistRetryAsync(
                        workItem,
                        targetSourcePosition,
                        "source_state_read_failed",
                        CreateUnexpectedFailureSummary("Projection source state read failed unexpectedly.", ex),
                        false,
                        cancellationToken);
                }

                continue;
            }

            foreach (BindingWorkItem workItem in pendingWorkItems)
            {
                ReplicaWriteRequest request;
                try
                {
                    request = BuildWriteRequest(workItem, sourceState, targetSourcePosition);
                }
                catch (Exception ex) when (!IsCriticalException(ex))
                {
                    Logger.ProjectionMappingFailed(workItem.DeliveryIdentity.DeliveryKey, targetSourcePosition, ex);
                    await PersistDeadLetterAsync(
                        workItem,
                        targetSourcePosition,
                        "mapping_failure",
                        CreateUnexpectedFailureSummary("Projection materialization failed unexpectedly.", ex),
                        cancellationToken);
                    continue;
                }

                ReplicaWriteResult result;
                try
                {
                    result = await workItem.Binding.ProviderHandle.WriteAsync(request, cancellationToken);
                }
                catch (ReplicaSinkWriteException ex) when (ex.Disposition == ReplicaSinkWriteFailureDisposition.Retry)
                {
                    Logger.ProviderWriteRetryScheduled(
                        workItem.DeliveryIdentity.DeliveryKey,
                        targetSourcePosition,
                        ex.FailureCode,
                        ex);
                    ApplyImmediateFailureContainment(workItem, ex.FailureCode, ex.FailureSummary);
                    await PersistRetryAsync(
                        workItem,
                        targetSourcePosition,
                        ex.FailureCode,
                        ex.FailureSummary,
                        true,
                        cancellationToken);
                    continue;
                }
                catch (ReplicaSinkWriteException ex)
                {
                    Logger.ProviderWriteDeadLettered(
                        workItem.DeliveryIdentity.DeliveryKey,
                        targetSourcePosition,
                        ex.FailureCode,
                        ex);
                    ApplyImmediateFailureContainment(workItem, ex.FailureCode, ex.FailureSummary);
                    await PersistDeadLetterAsync(
                        workItem,
                        targetSourcePosition,
                        ex.FailureCode,
                        ex.FailureSummary,
                        cancellationToken);
                    continue;
                }
                catch (Exception ex) when (!IsCriticalException(ex))
                {
                    Logger.ProviderWriteFailed(workItem.DeliveryIdentity.DeliveryKey, targetSourcePosition, ex);
                    await PersistRetryAsync(
                        workItem,
                        targetSourcePosition,
                        "provider_write_failed",
                        CreateUnexpectedFailureSummary("Replica sink provider write failed unexpectedly.", ex),
                        true,
                        cancellationToken);
                    continue;
                }

                await HandleTerminalWriteAsync(workItem, targetSourcePosition, result, cancellationToken);
            }
        }
    }

    private void ApplyImmediateFailureContainment(
        BindingWorkItem workItem,
        string failureCode,
        string failureSummary
    )
    {
        if (!IsAuthenticationFailureCode(failureCode))
        {
            return;
        }

        HealthManager.Park(
            workItem.Binding.Identity.SinkKey,
            failureCode,
            failureSummary,
            TimeProvider.GetUtcNow().Add(RuntimeOptions.Value.AuthFailureParkDuration));
    }

    private ReplicaSinkDeliveryState CreateDeadLetterState(
        ReplicaSinkDeliveryState currentState,
        long sourcePosition,
        string failureCode,
        string failureSummary
    )
    {
        DateTimeOffset now = TimeProvider.GetUtcNow();
        ReplicaSinkStoredFailure deadLetter = new(
            sourcePosition,
            GetNextAttemptCount(currentState, sourcePosition),
            failureCode,
            failureSummary,
            now);
        return new(
            currentState.DeliveryKey,
            currentState.DesiredSourcePosition,
            currentState.BootstrapUpperBoundSourcePosition,
            currentState.CommittedSourcePosition,
            null,
            deadLetter);
    }

    private ReplicaSinkDeliveryState CreateRetryState(
        ReplicaSinkDeliveryState currentState,
        long sourcePosition,
        string failureCode,
        string failureSummary
    )
    {
        DateTimeOffset now = TimeProvider.GetUtcNow();
        ReplicaSinkStoredFailure retry = new(
            sourcePosition,
            GetNextAttemptCount(currentState, sourcePosition),
            failureCode,
            failureSummary,
            now,
            now.Add(RetryDelay));
        return new(
            currentState.DeliveryKey,
            currentState.DesiredSourcePosition,
            currentState.BootstrapUpperBoundSourcePosition,
            currentState.CommittedSourcePosition,
            retry);
    }

    private ReplicaSinkBindingDescriptor[] GetBindings(
        Type projectionType
    ) =>
        Registry.GetBindingDescriptors().Where(binding => binding.ProjectionType == projectionType).ToArray();

    private async Task HandleTerminalWriteAsync(
        BindingWorkItem workItem,
        long sourcePosition,
        ReplicaWriteResult result,
        CancellationToken cancellationToken
    )
    {
        if (!IsTerminalOutcome(result.Outcome))
        {
            return;
        }

        await Hook.AfterProviderWriteBeforeCheckpointAsync(
            workItem.DeliveryIdentity,
            sourcePosition,
            cancellationToken);
        workItem.State = ReplicaSinkDeliveryStateTransitions.CreateCommittedState(workItem.State, sourcePosition);
        await DeliveryStateStore.WriteAsync(workItem.State, cancellationToken);
        Logger.DeliveryCheckpointed(workItem.DeliveryIdentity.DeliveryKey, sourcePosition);
    }

    private async Task<List<BindingWorkItem>> LoadWorkItemsAsync(
        ReplicaSinkBindingDescriptor[] bindings,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        List<BindingWorkItem> workItems = new(bindings.Length);
        foreach (ReplicaSinkBindingDescriptor binding in bindings)
        {
            ReplicaSinkDeliveryIdentity deliveryIdentity = new(binding.Identity, entityId);
            ReplicaSinkDeliveryState state = await ReadStateAsync(deliveryIdentity, cancellationToken);
            workItems.Add(new(binding, deliveryIdentity, state));
        }

        return workItems;
    }

    private async Task PersistDeadLetterAsync(
        BindingWorkItem workItem,
        long sourcePosition,
        string failureCode,
        string failureSummary,
        CancellationToken cancellationToken
    )
    {
        workItem.State = CreateDeadLetterState(workItem.State, sourcePosition, failureCode, failureSummary);
        try
        {
            await DeliveryStateStore.WriteAsync(workItem.State, cancellationToken);
            Logger.DeadLetterPersisted(workItem.DeliveryIdentity.DeliveryKey, sourcePosition, failureCode);
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            HealthManager.Quarantine(
                workItem.Binding.Identity.SinkKey,
                "dead_letter_store_failed",
                "Dead-letter persistence failed while storing terminal lane state.");
            Logger.DeadLetterStoreQuarantined(
                workItem.Binding.Identity.SinkKey,
                workItem.DeliveryIdentity.DeliveryKey,
                ex);
        }
    }

    private async Task PersistRetryAsync(
        BindingWorkItem workItem,
        long sourcePosition,
        string failureCode,
        string failureSummary,
        bool isProviderFailure,
        CancellationToken cancellationToken
    )
    {
        workItem.State = CreateRetryState(workItem.State, sourcePosition, failureCode, failureSummary);
        await DeliveryStateStore.WriteAsync(workItem.State, cancellationToken);
        if (workItem.State.Retry?.NextRetryAtUtc is DateTimeOffset nextRetryAtUtc)
        {
            Logger.RetryPersisted(workItem.DeliveryIdentity.DeliveryKey, sourcePosition, failureCode, nextRetryAtUtc);
            if (isProviderFailure && !IsAuthenticationFailureCode(failureCode) &&
                workItem.State.Retry.AttemptCount >= RuntimeOptions.Value.RepeatedProviderFailureThrottleThreshold)
            {
                HealthManager.Throttle(
                    workItem.Binding.Identity.SinkKey,
                    failureCode,
                    failureSummary,
                    TimeProvider.GetUtcNow().Add(RuntimeOptions.Value.RepeatedProviderFailureThrottleDuration));
            }
        }
    }

    private async Task<ReplicaSinkDeliveryState> ReadStateAsync(
        ReplicaSinkDeliveryIdentity deliveryIdentity,
        CancellationToken cancellationToken
    ) =>
        await DeliveryStateStore.ReadAsync(deliveryIdentity.DeliveryKey, cancellationToken) ??
        new ReplicaSinkDeliveryState(deliveryIdentity.DeliveryKey);

    private sealed class BindingWorkItem
    {
        public BindingWorkItem(
            ReplicaSinkBindingDescriptor binding,
            ReplicaSinkDeliveryIdentity deliveryIdentity,
            ReplicaSinkDeliveryState state
        )
        {
            Binding = binding;
            DeliveryIdentity = deliveryIdentity;
            State = state;
        }

        public ReplicaSinkBindingDescriptor Binding { get; }

        public ReplicaSinkDeliveryIdentity DeliveryIdentity { get; }

        public ReplicaSinkDeliveryState State { get; set; }
    }
}