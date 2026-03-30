using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

[Flags]
internal enum ReplicaSinkExecutionReason
{
    None = 0,
    Bootstrap = 1,
    Live = 2,
    Retry = 4,
}

internal readonly record struct ReplicaSinkExecutionKey(
    Type ProjectionType,
    string EntityId
);

internal sealed class ReplicaSinkRuntimeCoordinator : IReplicaSinkRuntimeCoordinator
{
    private static readonly ReplicaSinkExecutionReason[] ReasonOrder =
        [ReplicaSinkExecutionReason.Bootstrap, ReplicaSinkExecutionReason.Live, ReplicaSinkExecutionReason.Retry];

    public ReplicaSinkRuntimeCoordinator(
        IReplicaSinkLatestStateProcessor processor,
        IReplicaSinkProjectionRegistry registry,
        IReplicaSinkDeliveryStateStore deliveryStateStore,
        IReplicaSinkExecutionHealthManager healthManager,
        TimeProvider timeProvider,
        IOptions<ReplicaSinkRuntimeOptions> runtimeOptions
    )
    {
        Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        DeliveryStateStore = deliveryStateStore ?? throw new ArgumentNullException(nameof(deliveryStateStore));
        HealthManager = healthManager ?? throw new ArgumentNullException(nameof(healthManager));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        RuntimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
    }

    private Queue<ReplicaSinkExecutionKey> BootstrapQueue { get; } = [];

    private IReplicaSinkDeliveryStateStore DeliveryStateStore { get; }

    private SemaphoreSlim ExecutionGate { get; } = new(1, 1);

    private IReplicaSinkExecutionHealthManager HealthManager { get; }

    private Dictionary<ReplicaSinkExecutionKey, ReplicaSinkExecutionReason> PendingReasons { get; } = [];

    private IReplicaSinkLatestStateProcessor Processor { get; }

    private Queue<ReplicaSinkExecutionKey> RetryQueue { get; } = [];

    private IReplicaSinkProjectionRegistry Registry { get; }

    private int RoundRobinIndex { get; set; }

    private IOptions<ReplicaSinkRuntimeOptions> RuntimeOptions { get; }

    private object SyncRoot { get; } = new();

    private TimeProvider TimeProvider { get; }

    private Queue<ReplicaSinkExecutionKey> LiveQueue { get; } = [];

    public Task NotifyLiveAsync<TProjection>(
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class =>
        NotifyLiveAsync(typeof(TProjection), entityId, desiredSourcePosition, cancellationToken);

    public async Task NotifyLiveAsync(
        Type projectionType,
        string entityId,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentOutOfRangeException.ThrowIfNegative(desiredSourcePosition);
        await Processor.AdvanceDesiredPositionAsync(projectionType, entityId, desiredSourcePosition, cancellationToken);
        Enqueue(new(projectionType, entityId), ReplicaSinkExecutionReason.Live);
    }

    public Task RegisterBootstrapAsync<TProjection>(
        string entityId,
        string sinkKey,
        string targetName,
        long bootstrapUpperBoundSourcePosition,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
        where TProjection : class =>
        RegisterBootstrapAsync(
            typeof(TProjection),
            entityId,
            sinkKey,
            targetName,
            bootstrapUpperBoundSourcePosition,
            desiredSourcePosition,
            cancellationToken);

    public async Task RegisterBootstrapAsync(
        Type projectionType,
        string entityId,
        string sinkKey,
        string targetName,
        long bootstrapUpperBoundSourcePosition,
        long desiredSourcePosition,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentOutOfRangeException.ThrowIfNegative(bootstrapUpperBoundSourcePosition);
        ArgumentOutOfRangeException.ThrowIfNegative(desiredSourcePosition);
        ReplicaSinkBindingDescriptor binding = GetBindings(projectionType).SingleOrDefault(candidate =>
                string.Equals(candidate.Identity.SinkKey, sinkKey, StringComparison.Ordinal) &&
                string.Equals(candidate.Identity.TargetName, targetName, StringComparison.Ordinal)) ??
            throw new InvalidOperationException(
                $"Replica sink binding '{projectionType.FullName}|{sinkKey}|{targetName}' was not registered.");
        ReplicaSinkDeliveryIdentity deliveryIdentity = new(binding.Identity, entityId);
        ReplicaSinkDeliveryState currentState = await ReadStateAsync(binding, entityId, cancellationToken);
        ReplicaSinkDeliveryState updatedState = ReplicaSinkDeliveryStateTransitions.AdvanceDesiredPosition(
            currentState,
            desiredSourcePosition,
            deliveryIdentity,
            bootstrapUpperBoundSourcePosition);
        await DeliveryStateStore.WriteAsync(updatedState, cancellationToken);
        Enqueue(new(projectionType, entityId), ReplicaSinkExecutionReason.Bootstrap);
    }

    public async Task<int> ExecuteBatchAsync(
        CancellationToken cancellationToken = default
    )
    {
        await ExecutionGate.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            await HydrateDueRetriesAsync(now, cancellationToken);
            int processedCount = 0;
            int maxBatchSize = Math.Max(1, RuntimeOptions.Value.MaxExecutionBatchSize);
            Dictionary<string, int> processedPerSink = new(StringComparer.Ordinal);
            HashSet<ReplicaSinkExecutionKey> deferredKeys = [];
            while (processedCount < maxBatchSize &&
                   TryDequeueCandidate(deferredKeys, processedPerSink, out ReplicaSinkExecutionKey candidate))
            {
                EvaluatedReplicaSinkExecutionCandidate evaluation = await EvaluateCandidateAsync(
                    candidate,
                    now,
                    cancellationToken);
                if (!evaluation.CanRun)
                {
                    if (evaluation.ShouldRemainQueued && evaluation.PreferredReason != ReplicaSinkExecutionReason.None)
                    {
                        Enqueue(candidate, evaluation.PreferredReason);
                        deferredKeys.Add(candidate);
                    }

                    continue;
                }

                await Processor.FlushAsync(candidate.ProjectionType, candidate.EntityId, cancellationToken);
                processedCount++;
                IncrementSinkBudget(candidate, processedPerSink);
                now = TimeProvider.GetUtcNow();
                evaluation = await EvaluateCandidateAsync(candidate, now, cancellationToken);
                if ((evaluation.CanRun || evaluation.ShouldRemainQueued) &&
                    evaluation.PreferredReason != ReplicaSinkExecutionReason.None)
                {
                    Enqueue(candidate, evaluation.PreferredReason);
                }
            }

            return processedCount;
        }
        finally
        {
            _ = ExecutionGate.Release();
        }
    }

    private static ReplicaSinkExecutionReason ClassifyReason(
        ReplicaSinkDeliveryState state,
        long targetSourcePosition
    )
    {
        if (state.Retry?.SourcePosition == targetSourcePosition)
        {
            return ReplicaSinkExecutionReason.Retry;
        }

        if (state.BootstrapUpperBoundSourcePosition is not null &&
            (state.CommittedSourcePosition is null ||
             state.CommittedSourcePosition.Value < state.BootstrapUpperBoundSourcePosition.Value))
        {
            return ReplicaSinkExecutionReason.Bootstrap;
        }

        return ReplicaSinkExecutionReason.Live;
    }

    private void Enqueue(
        ReplicaSinkExecutionKey key,
        ReplicaSinkExecutionReason reason
    )
    {
        if (reason == ReplicaSinkExecutionReason.None)
        {
            return;
        }

        lock (SyncRoot)
        {
            PendingReasons.TryGetValue(key, out ReplicaSinkExecutionReason existingReasons);
            if ((existingReasons & reason) != 0)
            {
                return;
            }

            PendingReasons[key] = existingReasons | reason;
            GetQueue(reason).Enqueue(key);
        }
    }

    private async Task<EvaluatedReplicaSinkExecutionCandidate> EvaluateCandidateAsync(
        ReplicaSinkExecutionKey candidate,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        ReplicaSinkBindingDescriptor[] bindings = GetBindings(candidate.ProjectionType);
        if (bindings.Length == 0)
        {
            return EvaluatedReplicaSinkExecutionCandidate.None;
        }

        ReplicaSinkExecutionReason? blockedReason = null;
        ReplicaSinkExecutionReason? processableReason = null;
        foreach (ReplicaSinkBindingDescriptor binding in bindings)
        {
            ReplicaSinkDeliveryState state = await ReadStateAsync(binding, candidate.EntityId, cancellationToken);
            long? targetSourcePosition = ReplicaSinkDeliveryStateTransitions.GetEffectiveTargetSourcePosition(state);
            if (targetSourcePosition is null)
            {
                continue;
            }

            bool hasPendingWork = state.CommittedSourcePosition is null ||
                                  state.CommittedSourcePosition.Value < targetSourcePosition.Value;
            if (!hasPendingWork)
            {
                continue;
            }

            ReplicaSinkExecutionReason reason = ClassifyReason(state, targetSourcePosition.Value);
            if (HealthManager.IsBlocked(binding.Identity.SinkKey))
            {
                blockedReason ??= reason;
                continue;
            }

            if (ReplicaSinkDeliveryStateTransitions.HasProcessableWork(state, now))
            {
                processableReason ??= reason;
            }
        }

        if (processableReason is not null)
        {
            return new(true, false, processableReason.Value);
        }

        if (blockedReason is not null)
        {
            return new(false, true, blockedReason.Value);
        }

        return EvaluatedReplicaSinkExecutionCandidate.None;
    }

    private ReplicaSinkBindingDescriptor[] GetBindings(
        Type projectionType
    ) =>
        Registry.GetBindingDescriptors().Where(binding => binding.ProjectionType == projectionType).ToArray();

    private Queue<ReplicaSinkExecutionKey> GetQueue(
        ReplicaSinkExecutionReason reason
    ) =>
        reason switch
        {
            ReplicaSinkExecutionReason.Bootstrap => BootstrapQueue,
            ReplicaSinkExecutionReason.Live => LiveQueue,
            ReplicaSinkExecutionReason.Retry => RetryQueue,
            _ => throw new InvalidOperationException($"Unsupported execution reason '{reason}'."),
        };

    private IEnumerable<string> GetSinkKeys(
        ReplicaSinkExecutionKey key
    ) =>
        GetBindings(key.ProjectionType)
            .Select(binding => binding.Identity.SinkKey)
            .Distinct(StringComparer.Ordinal);

    private async Task HydrateDueRetriesAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        int maxRetrySelectionsPerBatch = Math.Max(1, RuntimeOptions.Value.MaxRetrySelectionsPerBatch);
        int maxRetrySelectionsPerSink = Math.Max(1, RuntimeOptions.Value.MaxRetrySelectionsPerSink);
        IReadOnlyList<ReplicaSinkDeliveryState> dueRetries = await DeliveryStateStore.ReadDueRetriesAsync(
            now,
            maxRetrySelectionsPerBatch,
            cancellationToken);
        Dictionary<string, int> selectedPerSink = new(StringComparer.Ordinal);
        foreach (ReplicaSinkDeliveryState state in dueRetries)
        {
            ReplicaSinkDeliveryKeyParser.ParsedReplicaSinkDeliveryKey parsedDeliveryKey = ReplicaSinkDeliveryKeyParser.Parse(
                state.DeliveryKey);
            if (HealthManager.IsBlocked(parsedDeliveryKey.SinkKey))
            {
                continue;
            }

            selectedPerSink.TryGetValue(parsedDeliveryKey.SinkKey, out int selectedCount);
            if (selectedCount >= maxRetrySelectionsPerSink)
            {
                continue;
            }

            if (!TryResolveProjectionType(parsedDeliveryKey.ProjectionTypeName, out Type? projectionType))
            {
                continue;
            }

            selectedPerSink[parsedDeliveryKey.SinkKey] = selectedCount + 1;
            Enqueue(new(projectionType, parsedDeliveryKey.EntityId), ReplicaSinkExecutionReason.Retry);
        }
    }

    private void IncrementSinkBudget(
        ReplicaSinkExecutionKey key,
        Dictionary<string, int> processedPerSink
    )
    {
        foreach (string sinkKey in GetSinkKeys(key))
        {
            processedPerSink.TryGetValue(sinkKey, out int processedCount);
            processedPerSink[sinkKey] = processedCount + 1;
        }
    }

    private bool HasSinkBudgetRemaining(
        ReplicaSinkExecutionKey key,
        Dictionary<string, int> processedPerSink
    )
    {
        int maxExecutionBatchSizePerSink = Math.Max(1, RuntimeOptions.Value.MaxExecutionBatchSizePerSink);
        foreach (string sinkKey in GetSinkKeys(key))
        {
            processedPerSink.TryGetValue(sinkKey, out int processedCount);
            if (processedCount >= maxExecutionBatchSizePerSink)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<ReplicaSinkDeliveryState> ReadStateAsync(
        ReplicaSinkBindingDescriptor binding,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        ReplicaSinkDeliveryIdentity deliveryIdentity = new(binding.Identity, entityId);
        return await DeliveryStateStore.ReadAsync(deliveryIdentity.DeliveryKey, cancellationToken) ??
               new ReplicaSinkDeliveryState(deliveryIdentity.DeliveryKey);
    }

    private bool TryDequeueCandidate(
        HashSet<ReplicaSinkExecutionKey> deferredKeys,
        Dictionary<string, int> processedPerSink,
        out ReplicaSinkExecutionKey candidate
    )
    {
        lock (SyncRoot)
        {
            for (int offset = 0; offset < ReasonOrder.Length; offset++)
            {
                int index = (RoundRobinIndex + offset) % ReasonOrder.Length;
                ReplicaSinkExecutionReason reason = ReasonOrder[index];
                if (TryDequeueCandidate(reason, deferredKeys, processedPerSink, out candidate))
                {
                    RoundRobinIndex = (index + 1) % ReasonOrder.Length;
                    return true;
                }
            }
        }

        candidate = default;
        return false;
    }

    private bool TryDequeueCandidate(
        ReplicaSinkExecutionReason reason,
        HashSet<ReplicaSinkExecutionKey> deferredKeys,
        Dictionary<string, int> processedPerSink,
        out ReplicaSinkExecutionKey candidate
    )
    {
        Queue<ReplicaSinkExecutionKey> queue = GetQueue(reason);
        int iterations = queue.Count;
        while (iterations-- > 0)
        {
            ReplicaSinkExecutionKey nextCandidate = queue.Dequeue();
            if (deferredKeys.Contains(nextCandidate))
            {
                queue.Enqueue(nextCandidate);
                continue;
            }

            if (!PendingReasons.TryGetValue(nextCandidate, out ReplicaSinkExecutionReason pendingReasons) ||
                (pendingReasons & reason) == 0)
            {
                continue;
            }

            if (!HasSinkBudgetRemaining(nextCandidate, processedPerSink))
            {
                queue.Enqueue(nextCandidate);
                deferredKeys.Add(nextCandidate);
                continue;
            }

            ReplicaSinkExecutionReason remainingReasons = pendingReasons & ~reason;
            if (remainingReasons == ReplicaSinkExecutionReason.None)
            {
                _ = PendingReasons.Remove(nextCandidate);
            }
            else
            {
                PendingReasons[nextCandidate] = remainingReasons;
            }

            candidate = nextCandidate;
            return true;
        }

        candidate = default;
        return false;
    }

    private bool TryResolveProjectionType(
        string projectionTypeName,
        out Type? projectionType
    )
    {
        projectionType = Registry.GetBindingDescriptors()
            .Where(binding => string.Equals(binding.Identity.ProjectionTypeName, projectionTypeName, StringComparison.Ordinal))
            .Select(binding => binding.ProjectionType)
            .FirstOrDefault();
        return projectionType is not null;
    }

    private readonly record struct EvaluatedReplicaSinkExecutionCandidate(
        bool CanRun,
        bool ShouldRemainQueued,
        ReplicaSinkExecutionReason PreferredReason
    )
    {
        public static EvaluatedReplicaSinkExecutionCandidate None => new(false, false, ReplicaSinkExecutionReason.None);
    }
}
