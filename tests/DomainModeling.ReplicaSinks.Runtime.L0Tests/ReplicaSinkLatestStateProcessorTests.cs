using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

using Moq;

using ReplicaStateKey = (System.Type ProjectionType, string EntityId, long SourcePosition);


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests the Increment 03a durable latest-state processor.
/// </summary>
public sealed class ReplicaSinkLatestStateProcessorTests
{
    private static readonly long[] ExpectedDeadLetterSupersessionSourcePositions = [30L, 31L];

    private static readonly long[] ExpectedRetrySupersessionSourcePositions = [20L, 21L];

    private static ReplicaSinkBindingDescriptor CreateMappedBinding<TProjection>(
        string sinkKey,
        string targetName,
        IReplicaSinkProvider provider,
        Func<TProjection, object> map
    )
        where TProjection : class =>
        new(
            new(typeof(TProjection).FullName ?? typeof(TProjection).Name, sinkKey, targetName),
            typeof(TProjection),
            ReplicaWriteMode.LatestState,
            typeof(TestContract),
            "TestApp.Replica.TestContract.V1",
            input => map((TProjection)input),
            false,
            new(sinkKey, "client", provider.Format, provider.GetType(), ReplicaProvisioningMode.CreateIfMissing),
            provider,
            new(new("client", targetName), ReplicaProvisioningMode.CreateIfMissing));

    private static ReplicaSinkBindingDescriptor CreateDirectBinding<TProjection>(
        string sinkKey,
        string targetName,
        IReplicaSinkProvider provider
    )
        where TProjection : class =>
        new(
            new(typeof(TProjection).FullName ?? typeof(TProjection).Name, sinkKey, targetName),
            typeof(TProjection),
            ReplicaWriteMode.LatestState,
            null,
            typeof(TProjection).FullName ?? typeof(TProjection).Name,
            null,
            true,
            new(sinkKey, "client", provider.Format, provider.GetType(), ReplicaProvisioningMode.CreateIfMissing),
            provider,
            new(new("client", targetName), ReplicaProvisioningMode.CreateIfMissing));

    private static ReplicaSinkLatestStateProcessor CreateProcessor(
        IReadOnlyList<ReplicaSinkBindingDescriptor> bindings,
        IReplicaSinkDeliveryStateStore stateStore,
        IReplicaSinkSourceStateAccessor sourceStateAccessor,
        TimeProvider? timeProvider = null,
        IReplicaSinkLatestStateProcessorHook? hook = null,
        IReplicaSinkExecutionHealthManager? healthManager = null,
        ReplicaSinkRuntimeOptions? runtimeOptions = null
    )
    {
        TimeProvider effectiveTimeProvider = timeProvider ?? TimeProvider.System;
        return new(
            new TestReplicaSinkProjectionRegistry(bindings),
            stateStore,
            sourceStateAccessor,
            effectiveTimeProvider,
            hook ?? new NullReplicaSinkLatestStateProcessorHook(),
            healthManager ?? new ReplicaSinkExecutionHealthManager(effectiveTimeProvider),
            Options.Create(runtimeOptions ?? new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkLatestStateProcessor>.Instance);
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        FakeTimeProvider timeProvider = new();
        timeProvider.SetUtcNow(new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
        return timeProvider;
    }

    /// <summary>
    ///     Verifies delete-like source states produce delete writes and committed checkpoints.
    /// </summary>
    /// <param name="sourceState">The delete-like source state to exercise.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task FlushAsyncShouldWriteDeleteForDeleteLikeSourceStateAsync(
        ReplicaSinkSourceState sourceState
    )
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetState(typeof(TestProjection), "entity-1", 12, sourceState);
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 12, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaWriteRequest request = Assert.Single(provider.Requests);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.True(request.IsDeleted);
        Assert.Null(request.Payload);
        Assert.Equal(12, request.SourcePosition);
        Assert.NotNull(state);
        Assert.Equal(12, state.CommittedSourcePosition);
    }

    private static string GetDeliveryKey(
        ReplicaSinkBindingDescriptor binding,
        string entityId
    ) =>
        new ReplicaSinkDeliveryIdentity(binding.Identity, entityId).DeliveryKey;

    /// <summary>
    ///     Replays the already-applied bootstrap write to confirm the provider reports a duplicate outcome.
    /// </summary>
    /// <param name="provider">The bootstrap provider.</param>
    /// <param name="binding">The binding being replayed.</param>
    /// <returns>The duplicate outcome observed from the replay write.</returns>
    private static async Task<ReplicaWriteOutcome> GetDuplicateOutcomeAsync(
        BootstrapReplicaSinkProvider provider,
        ReplicaSinkBindingDescriptor binding
    )
    {
        ReplicaWriteResult result = await provider.WriteAsync(
            new(
                binding.Target,
                GetDeliveryKey(binding, "entity-1"),
                40,
                ReplicaWriteMode.LatestState,
                binding.ContractIdentity,
                new TestContract
                {
                    Id = "projection-40",
                }),
            CancellationToken.None);
        return result.Outcome;
    }

    private sealed class InMemoryReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
    {
        private ConcurrentDictionary<string, ReplicaSinkDeliveryState> States { get; } = new(StringComparer.Ordinal);

        public Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
            int pageSize,
            string? continuationToken = null,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            int offset = string.IsNullOrWhiteSpace(continuationToken)
                ? 0
                : int.Parse(continuationToken, CultureInfo.InvariantCulture);
            List<ReplicaSinkDeliveryState> orderedStates = States.Values
                .Where(static state => state.DeadLetter is not null)
                .OrderByDescending(static state => state.DeadLetter!.RecordedAtUtc)
                .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
                .ToList();
            List<ReplicaSinkDeliveryState> items = orderedStates.Skip(offset).Take(pageSize).ToList();
            int nextOffset = offset + items.Count;
            return Task.FromResult(new ReplicaSinkDeliveryStatePage(
                items,
                nextOffset < orderedStates.Count ? nextOffset.ToString(CultureInfo.InvariantCulture) : null));
        }

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
            DateTimeOffset dueAtOrBeforeUtc,
            int maxCount,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<ReplicaSinkDeliveryState> items = States.Values
                .Where(state => state.Retry?.NextRetryAtUtc is DateTimeOffset nextRetryAtUtc && nextRetryAtUtc <= dueAtOrBeforeUtc)
                .OrderBy(static state => state.Retry!.NextRetryAtUtc)
                .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
                .Take(maxCount)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<ReplicaSinkDeliveryState?> ReadAsync(
            string deliveryKey,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = States.TryGetValue(deliveryKey, out ReplicaSinkDeliveryState? state);
            return Task.FromResult(state);
        }

        public Task WriteAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            States[state.DeliveryKey] = state;
            return Task.CompletedTask;
        }
    }

    private sealed class DeadLetterWriteFailingReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
    {
        private InMemoryReplicaSinkDeliveryStateStore InnerStore { get; } = new();

        public Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
            int pageSize,
            string? continuationToken = null,
            CancellationToken cancellationToken = default
        ) =>
            InnerStore.ReadDeadLetterPageAsync(pageSize, continuationToken, cancellationToken);

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
            DateTimeOffset dueAtOrBeforeUtc,
            int maxCount,
            CancellationToken cancellationToken = default
        ) =>
            InnerStore.ReadDueRetriesAsync(dueAtOrBeforeUtc, maxCount, cancellationToken);

        public Task<ReplicaSinkDeliveryState?> ReadAsync(
            string deliveryKey,
            CancellationToken cancellationToken = default
        ) =>
            InnerStore.ReadAsync(deliveryKey, cancellationToken);

        public Task WriteAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        )
        {
            if (state.DeadLetter is not null)
            {
                throw new InvalidOperationException("Synthetic dead-letter persistence failure.");
            }

            return InnerStore.WriteAsync(state, cancellationToken);
        }
    }

    private sealed class RecordingReplicaSinkProvider : IReplicaSinkProvider
    {
        public RecordingReplicaSinkProvider(
            Func<ReplicaWriteRequest, ReplicaWriteResult> onWrite
        ) =>
            OnWrite = onWrite;

        public string Format => "test";

        public List<ReplicaWriteRequest> Requests { get; } = [];

        private Func<ReplicaWriteRequest, ReplicaWriteResult> OnWrite { get; }

        public ValueTask EnsureTargetAsync(
            ReplicaTargetDescriptor target,
            CancellationToken cancellationToken
        ) =>
            ValueTask.CompletedTask;

        public ValueTask<ReplicaTargetInspection> InspectAsync(
            ReplicaTargetDescriptor target,
            CancellationToken cancellationToken
        ) =>
            ValueTask.FromResult(new ReplicaTargetInspection(target.DestinationIdentity, true, Requests.Count));

        public ValueTask<ReplicaWriteResult> WriteAsync(
            ReplicaWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            return ValueTask.FromResult(OnWrite(request));
        }
    }

    private sealed class TestReplicaSinkProjectionRegistry : IReplicaSinkProjectionRegistry
    {
        public TestReplicaSinkProjectionRegistry(
            IReadOnlyList<ReplicaSinkBindingDescriptor> bindings
        ) =>
            Bindings = bindings;

        private IReadOnlyList<ReplicaSinkBindingDescriptor> Bindings { get; }

        public IReadOnlyList<ReplicaSinkBindingDescriptor> GetBindingDescriptors() => Bindings;

        public IReadOnlyList<ReplicaSinkStartupDiagnostic> GetDiagnostics() => [];
    }

    private sealed class TestReplicaSinkSourceStateAccessor : IReplicaSinkSourceStateAccessor
    {
        public List<(Type ProjectionType, string EntityId, long SourcePosition)> ReadRequests { get; } = [];

        private Dictionary<ReplicaStateKey, Exception> Failures { get; } = [];

        private Dictionary<ReplicaStateKey, ReplicaSinkSourceState> States { get; } = [];

        public ValueTask<ReplicaSinkSourceState> ReadAsync(
            Type projectionType,
            string entityId,
            long sourcePosition,
            CancellationToken cancellationToken = default
        )
        {
            ReadRequests.Add((projectionType, entityId, sourcePosition));
            if (Failures.TryGetValue((projectionType, entityId, sourcePosition), out Exception? failure))
            {
                throw failure;
            }

            if (!States.TryGetValue((projectionType, entityId, sourcePosition), out ReplicaSinkSourceState? state))
            {
                throw new ReplicaSinkSourceStateUnavailableException(projectionType, entityId, sourcePosition, null);
            }

            return ValueTask.FromResult(state);
        }

        public void SetState(
            Type projectionType,
            string entityId,
            long sourcePosition,
            ReplicaSinkSourceState state
        ) =>
            States[(projectionType, entityId, sourcePosition)] = state;

        public void SetFailure(
            Type projectionType,
            string entityId,
            long sourcePosition,
            Exception failure
        )
        {
            ArgumentNullException.ThrowIfNull(failure);
            Failures[(projectionType, entityId, sourcePosition)] = failure;
        }

        public void SetValue(
            Type projectionType,
            string entityId,
            long sourcePosition,
            object value
        ) =>
            SetState(projectionType, entityId, sourcePosition, ReplicaSinkSourceState.FromValue(sourcePosition, value));
    }

    private sealed class ThrowAfterWriteHook : IReplicaSinkLatestStateProcessorHook
    {
        public Task AfterProviderWriteBeforeCheckpointAsync(
            ReplicaSinkDeliveryIdentity deliveryIdentity,
            long sourcePosition,
            CancellationToken cancellationToken = default
        ) =>
            throw new InvalidOperationException("Synthetic crash after provider write.");
    }

    /// <summary>
    ///     AdvanceDesiredPositionAsync rejects rewinds for the same delivery lane.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AdvanceDesiredPositionAsyncShouldRejectRewindForSameBinding()
    {
        RecordingReplicaSinkProvider provider = new(_ => new(ReplicaWriteOutcome.Applied, new("client", "orders"), 10));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            new TestReplicaSinkSourceStateAccessor());
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 10, CancellationToken.None);
        ReplicaSinkRewindRejectedException exception =
            await Assert.ThrowsAsync<ReplicaSinkRewindRejectedException>(async () =>
                await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 9, CancellationToken.None));
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(10, state.DesiredSourcePosition);
        Assert.Null(state.CommittedSourcePosition);
        Assert.Equal(9, exception.RequestedSourcePosition);
    }

    /// <summary>
    ///     A replay after a post-write crash checkpoints the already-applied latest-state delivery without duplicating the
    ///     write.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldCheckpointAfterSafeReplayWhenWriteSucceedsBeforeCrash()
    {
        BootstrapReplicaSinkProvider provider = new(
            "bootstrap",
            new()
            {
                ClientKey = "client",
                ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing,
            },
            NullLogger<BootstrapReplicaSinkProvider>.Instance);
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            40,
            new TestProjection
            {
                Id = "projection-40",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        await provider.EnsureTargetAsync(binding.Target, CancellationToken.None);
        ReplicaSinkLatestStateProcessor failingProcessor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            hook: new ThrowAfterWriteHook());
        await failingProcessor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 40, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await failingProcessor.FlushAsync<TestProjection>("entity-1", CancellationToken.None));
        ReplicaSinkDeliveryState? failedState = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(failedState);
        Assert.Equal(40, failedState.DesiredSourcePosition);
        Assert.Null(failedState.CommittedSourcePosition);
        ReplicaSinkLatestStateProcessor replayProcessor = CreateProcessor([binding], stateStore, sourceAccessor);
        await replayProcessor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? replayedState = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        ReplicaTargetInspection inspection = await provider.InspectAsync(binding.Target, CancellationToken.None);
        Assert.NotNull(replayedState);
        Assert.Equal(40, replayedState.CommittedSourcePosition);
        Assert.Equal(ReplicaWriteOutcome.DuplicateIgnored, await GetDuplicateOutcomeAsync(provider, binding));
        Assert.Equal(1, inspection.WriteCount);
        Assert.Equal(40, inspection.LatestSourcePosition);
    }

    /// <summary>
    ///     A newer successful desired position clears dead-letter state left behind by an older superseded attempt.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldClearSupersededDeadLetterStateWhenNewerDesiredPositionSucceeds()
    {
        RecordingReplicaSinkProvider provider = new(request =>
        {
            if (request.SourcePosition == 30)
            {
                throw new ReplicaSinkWriteException(
                    ReplicaSinkWriteFailureDisposition.DeadLetter,
                    "validation_failed",
                    "Validation failed.");
            }

            return new(ReplicaWriteOutcome.Applied, request.Target.DestinationIdentity, request.SourcePosition);
        });
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            30,
            new TestProjection
            {
                Id = "projection-30",
            });
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            31,
            new TestProjection
            {
                Id = "projection-31",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 30, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 31, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(31, state.DesiredSourcePosition);
        Assert.Equal(31, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Equal(
            ExpectedDeadLetterSupersessionSourcePositions,
            provider.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     A newer successful desired position clears retry state left behind by an older superseded attempt.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldClearSupersededRetryStateWhenNewerDesiredPositionSucceeds()
    {
        RecordingReplicaSinkProvider provider = new(request =>
        {
            if (request.SourcePosition == 20)
            {
                throw new ReplicaSinkWriteException(
                    ReplicaSinkWriteFailureDisposition.Retry,
                    "transient_failure",
                    "Transient provider failure.");
            }

            return new(ReplicaWriteOutcome.Applied, request.Target.DestinationIdentity, request.SourcePosition);
        });
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            20,
            new TestProjection
            {
                Id = "projection-20",
            });
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            21,
            new TestProjection
            {
                Id = "projection-21",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 20, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 21, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(21, state.DesiredSourcePosition);
        Assert.Equal(21, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Equal(
            ExpectedRetrySupersessionSourcePositions,
            provider.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     FlushAsync coalesces pending work to the highest desired source position before reading and mapping.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldCoalesceToHighestDesiredPositionBeforeMaterializing()
    {
        int mapCount = 0;
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection =>
            {
                mapCount++;
                return new TestContract
                {
                    Id = projection.Id,
                };
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            6,
            new TestProjection
            {
                Id = "projection-6",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 5, CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 6, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(6, state.DesiredSourcePosition);
        Assert.Equal(6, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Single(provider.Requests);
        Assert.Equal(6, provider.Requests.Single().SourcePosition);
        Assert.Single(sourceAccessor.ReadRequests);
        Assert.Equal(6, sourceAccessor.ReadRequests.Single().SourcePosition);
        Assert.Equal(1, mapCount);
    }

    /// <summary>
    ///     FlushAsync respects a bootstrap cutover fence before processing later live positions for the same lane.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldRespectBootstrapUpperBoundBeforeCutover()
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            10,
            new TestProjection
            {
                Id = "projection-10",
            });
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            12,
            new TestProjection
            {
                Id = "projection-12",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(binding, "entity-1"),
                desiredSourcePosition: 12,
                bootstrapUpperBoundSourcePosition: 10),
            CancellationToken.None);
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? firstState = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(firstState);
        Assert.Equal(12, firstState.DesiredSourcePosition);
        Assert.Equal(10, firstState.CommittedSourcePosition);
        Assert.Null(firstState.BootstrapUpperBoundSourcePosition);
        Assert.Equal([10L], provider.Requests.Select(static request => request.SourcePosition).ToArray());
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? secondState = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(secondState);
        Assert.Equal(12, secondState.CommittedSourcePosition);
        Assert.Equal([10L, 12L], provider.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     FlushAsync coalesces to the highest desired source position before directly materializing the projection.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldDirectlyMaterializeProjectionAfterSupersessionDecision()
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateDirectBinding<TestProjection>(
            "sink-a",
            "orders-direct",
            provider);
        TestProjection projection = new()
        {
            Id = "projection-31",
        };
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 31, projection);
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 30, CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 31, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        ReplicaWriteRequest request = Assert.Single(provider.Requests);
        Assert.NotNull(state);
        Assert.Equal(31, state.DesiredSourcePosition);
        Assert.Equal(31, state.CommittedSourcePosition);
        Assert.False(request.IsDeleted);
        Assert.Equal(31, request.SourcePosition);
        Assert.Same(projection, request.Payload);
        Assert.Single(sourceAccessor.ReadRequests);
        Assert.Equal(31, sourceAccessor.ReadRequests.Single().SourcePosition);
    }

    /// <summary>
    ///     FlushAsync persists dead-letter state when the provider signals a terminal write failure.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldPersistDeadLetterStateForTerminalWriteFailure()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(_ => throw new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.DeadLetter,
            "validation_failed",
            "Validation failed."));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            16,
            new TestProjection
            {
                Id = "projection-16",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            timeProvider);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 16, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(16, state.DesiredSourcePosition);
        Assert.Null(state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.NotNull(state.DeadLetter);
        Assert.Equal(16, state.DeadLetter.SourcePosition);
        Assert.Equal(1, state.DeadLetter.AttemptCount);
        Assert.Equal("validation_failed", state.DeadLetter.FailureCode);
        Assert.Null(state.DeadLetter.NextRetryAtUtc);
        Assert.Equal(timeProvider.GetUtcNow(), state.DeadLetter.RecordedAtUtc);
    }

    /// <summary>
    ///     FlushAsync persists a sanitized dead-letter record when unexpected mapping failures occur.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldPersistSanitizedDeadLetterForUnexpectedMappingFailure()
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            _ => throw new InvalidOperationException("sensitive mapping failure"));
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            18,
            new TestProjection
            {
                Id = "projection-18",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 18, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(18, state.DesiredSourcePosition);
        Assert.Null(state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.NotNull(state.DeadLetter);
        Assert.Equal(18, state.DeadLetter.SourcePosition);
        Assert.Equal("mapping_failure", state.DeadLetter.FailureCode);
        Assert.Equal(
            "Projection materialization failed unexpectedly. (InvalidOperationException).",
            state.DeadLetter.FailureSummary);
        Assert.DoesNotContain("sensitive mapping failure", state.DeadLetter.FailureSummary, StringComparison.Ordinal);
        Assert.Empty(provider.Requests);
    }

    /// <summary>
    ///     FlushAsync persists retry state when the provider signals a retryable write failure.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldPersistRetryStateForRetryableWriteFailure()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(_ => throw new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            "transient_failure",
            "Transient provider failure."));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            15,
            new TestProjection
            {
                Id = "projection-15",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            timeProvider);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 15, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(15, state.DesiredSourcePosition);
        Assert.Null(state.CommittedSourcePosition);
        Assert.NotNull(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Equal(15, state.Retry.SourcePosition);
        Assert.Equal(1, state.Retry.AttemptCount);
        Assert.Equal("transient_failure", state.Retry.FailureCode);
        Assert.Equal(timeProvider.GetUtcNow().AddMinutes(1), state.Retry.NextRetryAtUtc);
    }

    /// <summary>
    ///     FlushAsync does not re-run a retryable lane until the persisted retry due time arrives.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldSkipRetryUntilDueTimeArrives()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            15,
            new TestProjection
            {
                Id = "projection-15",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(binding, "entity-1"),
                desiredSourcePosition: 15,
                retry: new(
                    15,
                    1,
                    "transient_failure",
                    "Transient provider failure.",
                    timeProvider.GetUtcNow(),
                    timeProvider.GetUtcNow().AddMinutes(1))),
            CancellationToken.None);
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor, timeProvider);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Empty(provider.Requests);
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Single(provider.Requests);
        Assert.Equal(15, provider.Requests.Single().SourcePosition);
    }

    /// <summary>
    ///     FlushAsync keeps the last committed checkpoint while persisting a sanitized retry record for a newer desired
    ///     position.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldKeepCommittedPositionWhilePersistingSanitizedRetryForNewerDesiredPosition()
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            10,
            new TestProjection
            {
                Id = "projection-10",
            });
        sourceAccessor.SetFailure(
            typeof(TestProjection),
            "entity-1",
            11,
            new InvalidOperationException("sensitive source-state failure"));
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 10, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 11, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(11, state.DesiredSourcePosition);
        Assert.Equal(10, state.CommittedSourcePosition);
        Assert.NotNull(state.Retry);
        Assert.Equal(11, state.Retry.SourcePosition);
        Assert.Equal("source_state_read_failed", state.Retry.FailureCode);
        Assert.Equal(
            "Projection source state read failed unexpectedly. (InvalidOperationException).",
            state.Retry.FailureSummary);
        Assert.DoesNotContain("sensitive source-state failure", state.Retry.FailureSummary, StringComparison.Ordinal);
        Assert.Null(state.DeadLetter);
        Assert.Single(provider.Requests);
        Assert.Equal(10, provider.Requests.Single().SourcePosition);
    }

    /// <summary>
    ///     FlushAsync checkpoints terminal provider outcomes when the target reports the write as superseded.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldCheckpointSupersededIgnoredTerminalOutcome()
    {
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.SupersededIgnored,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            22,
            new TestProjection
            {
                Id = "projection-22",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 22, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(22, state.DesiredSourcePosition);
        Assert.Equal(22, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Single(provider.Requests);
        Assert.Equal(22, provider.Requests.Single().SourcePosition);
    }

    /// <summary>
    ///     Retryable authentication failures park the sink so repeated flushes do not hot-loop the provider.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldParkSinkAfterAuthenticationFailure()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(_ => throw new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            "authentication_failed",
            "Authentication failed."));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            25,
            new TestProjection
            {
                Id = "projection-25",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkExecutionHealthManager healthManager = new(timeProvider);
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            timeProvider,
            healthManager: healthManager);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 25, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkExecutionBlock? block = healthManager.GetCurrentBlock("sink-a");
        Assert.NotNull(block);
        Assert.Equal(ReplicaSinkExecutionBlockKind.Parked, block.Kind);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Single(provider.Requests);
    }

    /// <summary>
    ///     Repeated retryable provider failures throttle the sink until the configured block duration expires.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldThrottleSinkAfterRepeatedRetryableProviderFailures()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(_ => throw new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            "transient_failure",
            "Transient provider failure."));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            27,
            new TestProjection
            {
                Id = "projection-27",
            });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkExecutionHealthManager healthManager = new(timeProvider);
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            timeProvider,
            healthManager: healthManager,
            runtimeOptions: new ReplicaSinkRuntimeOptions
            {
                RepeatedProviderFailureThrottleThreshold = 2,
                RepeatedProviderFailureThrottleDuration = TimeSpan.FromMinutes(5),
            });
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 27, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Null(healthManager.GetCurrentBlock("sink-a"));
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkExecutionBlock? block = healthManager.GetCurrentBlock("sink-a");
        Assert.NotNull(block);
        Assert.Equal(ReplicaSinkExecutionBlockKind.Throttled, block.Kind);
        Assert.Equal(2, provider.Requests.Count);
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Equal(2, provider.Requests.Count);
        timeProvider.Advance(TimeSpan.FromMinutes(4));
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        Assert.Equal(3, provider.Requests.Count);
    }

    /// <summary>
    ///     Dead-letter persistence failures quarantine the sink instead of repeatedly retrying unsafe partial work.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldQuarantineSinkWhenDeadLetterPersistenceFails()
    {
        RecordingReplicaSinkProvider provider = new(_ => throw new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.DeadLetter,
            "validation_failed",
            "Validation failed."));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract
            {
                Id = projection.Id,
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(
            typeof(TestProjection),
            "entity-1",
            26,
            new TestProjection
            {
                Id = "projection-26",
            });
        DeadLetterWriteFailingReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkExecutionHealthManager healthManager = new(TimeProvider.System);
        ReplicaSinkLatestStateProcessor processor = CreateProcessor(
            [binding],
            stateStore,
            sourceAccessor,
            healthManager: healthManager);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 26, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkExecutionBlock? block = healthManager.GetCurrentBlock("sink-a");
        Assert.NotNull(block);
        Assert.Equal(ReplicaSinkExecutionBlockKind.Quarantined, block.Kind);
    }

    /// <summary>
    ///     FlushAsync writes delete semantics when the shared source state is absent.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldWriteDeleteForAbsentSourceState()
    {
        await FlushAsyncShouldWriteDeleteForDeleteLikeSourceStateAsync(ReplicaSinkSourceState.Absent(12));
    }

    /// <summary>
    ///     FlushAsync writes delete semantics when the shared source state is explicitly deleted.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldWriteDeleteForDeletedSourceState()
    {
        await FlushAsyncShouldWriteDeleteForDeleteLikeSourceStateAsync(ReplicaSinkSourceState.Deleted(12));
    }

    /// <summary>
    ///     The UX projection source-state accessor reports deleted when the projection exists at the version but returns null
    ///     state.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task
        ReplicaSinkUxProjectionSourceStateAccessorShouldReturnDeletedWhenProjectionStateIsNullAtAvailablePosition()
    {
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        projectionGrain.Setup(grain => grain.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(50));
        projectionGrain
            .Setup(grain => grain.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> grainFactory = new();
        grainFactory.Setup(factory => factory.GetUxProjectionGrain<TestProjection>("entity-1"))
            .Returns(projectionGrain.Object);
        ReplicaSinkUxProjectionSourceStateAccessor accessor = new(grainFactory.Object);
        ReplicaSinkSourceState state = await accessor.ReadAsync(
            typeof(TestProjection),
            "entity-1",
            50,
            CancellationToken.None);
        Assert.Equal(ReplicaSinkSourceStateKind.Deleted, state.Kind);
        Assert.Equal(50, state.SourcePosition);
    }

    /// <summary>
    ///     The UX projection source-state accessor rejects requests that are ahead of the latest available version.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkUxProjectionSourceStateAccessorShouldThrowWhenRequestedPositionIsAheadOfLatestVersion()
    {
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        projectionGrain.Setup(grain => grain.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(51));
        Mock<IUxProjectionGrainFactory> grainFactory = new();
        grainFactory.Setup(factory => factory.GetUxProjectionGrain<TestProjection>("entity-1"))
            .Returns(projectionGrain.Object);
        ReplicaSinkUxProjectionSourceStateAccessor accessor = new(grainFactory.Object);
        ReplicaSinkSourceStateUnavailableException exception =
            await Assert.ThrowsAsync<ReplicaSinkSourceStateUnavailableException>(async () =>
                await accessor.ReadAsync(typeof(TestProjection), "entity-1", 52, CancellationToken.None));
        Assert.Equal(52, exception.RequestedSourcePosition);
        Assert.Equal(51, exception.LatestAvailableSourcePosition);
    }
}