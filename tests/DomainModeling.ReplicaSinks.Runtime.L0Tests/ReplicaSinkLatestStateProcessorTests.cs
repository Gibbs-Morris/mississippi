using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

using Moq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests the Increment 03a durable latest-state processor.
/// </summary>
public sealed class ReplicaSinkLatestStateProcessorTests
{
    private static readonly long[] ExpectedDeadLetterSupersessionSourcePositions = [30L, 31L];
    private static readonly long[] ExpectedRetrySupersessionSourcePositions = [20L, 21L];

    /// <summary>
    ///     AdvanceDesiredPositionAsync rejects rewinds for the same delivery lane.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AdvanceDesiredPositionAsyncShouldRejectRewindForSameBinding()
    {
        RecordingReplicaSinkProvider provider = new(_ =>
            new ReplicaWriteResult(
                ReplicaWriteOutcome.Applied,
                new ReplicaDestinationIdentity("client", "orders"),
                10));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract { Id = projection.Id });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, new TestReplicaSinkSourceStateAccessor());

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 10, CancellationToken.None);
        ReplicaSinkRewindRejectedException exception = await Assert.ThrowsAsync<ReplicaSinkRewindRejectedException>(async () =>
            await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 9, CancellationToken.None));
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(10, state.DesiredSourcePosition);
        Assert.Null(state.CommittedSourcePosition);
        Assert.Equal(9, exception.RequestedSourcePosition);
    }

    /// <summary>
    ///     FlushAsync coalesces pending work to the highest desired source position before reading and mapping.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldCoalesceToHighestDesiredPositionBeforeMaterializing()
    {
        int mapCount = 0;
        RecordingReplicaSinkProvider provider = new(request =>
            new ReplicaWriteResult(
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
                return new TestContract { Id = projection.Id };
            });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 6, new TestProjection { Id = "projection-6" });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 5, CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 6, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

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
    ///     FlushAsync writes delete semantics when the shared source state is explicitly deleted.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldWriteDeleteForDeletedSourceState()
    {
        await FlushAsyncShouldWriteDeleteForDeleteLikeSourceStateAsync(ReplicaSinkSourceState.Deleted(12));
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
    ///     Verifies delete-like source states produce delete writes and committed checkpoints.
    /// </summary>
    /// <param name="sourceState">The delete-like source state to exercise.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task FlushAsyncShouldWriteDeleteForDeleteLikeSourceStateAsync(
        ReplicaSinkSourceState sourceState
    )
    {
        RecordingReplicaSinkProvider provider = new(request =>
            new ReplicaWriteResult(
                ReplicaWriteOutcome.Applied,
                request.Target.DestinationIdentity,
                request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetState(typeof(TestProjection), "entity-1", 12, sourceState);
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 12, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaWriteRequest request = Assert.Single(provider.Requests);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

        Assert.True(request.IsDeleted);
        Assert.Null(request.Payload);
        Assert.Equal(12, request.SourcePosition);
        Assert.NotNull(state);
        Assert.Equal(12, state.CommittedSourcePosition);
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
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 15, new TestProjection { Id = "projection-15" });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor, timeProvider);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 15, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

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
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 16, new TestProjection { Id = "projection-16" });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor, timeProvider);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 16, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

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

            return new ReplicaWriteResult(
                ReplicaWriteOutcome.Applied,
                request.Target.DestinationIdentity,
                request.SourcePosition);
        });
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 20, new TestProjection { Id = "projection-20" });
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 21, new TestProjection { Id = "projection-21" });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 20, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 21, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(21, state.DesiredSourcePosition);
        Assert.Equal(21, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Equal(ExpectedRetrySupersessionSourcePositions, provider.Requests.Select(static request => request.SourcePosition).ToArray());
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

            return new ReplicaWriteResult(
                ReplicaWriteOutcome.Applied,
                request.Target.DestinationIdentity,
                request.SourcePosition);
        });
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 30, new TestProjection { Id = "projection-30" });
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 31, new TestProjection { Id = "projection-31" });
        InMemoryReplicaSinkDeliveryStateStore stateStore = new();
        ReplicaSinkLatestStateProcessor processor = CreateProcessor([binding], stateStore, sourceAccessor);

        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 30, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        await processor.AdvanceDesiredPositionAsync<TestProjection>("entity-1", 31, CancellationToken.None);
        await processor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? state = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(31, state.DesiredSourcePosition);
        Assert.Equal(31, state.CommittedSourcePosition);
        Assert.Null(state.Retry);
        Assert.Null(state.DeadLetter);
        Assert.Equal(ExpectedDeadLetterSupersessionSourcePositions, provider.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     A replay after a post-write crash checkpoints the already-applied latest-state delivery without duplicating the write.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FlushAsyncShouldCheckpointAfterSafeReplayWhenWriteSucceedsBeforeCrash()
    {
        BootstrapReplicaSinkProvider provider = new(
            "bootstrap",
            new BootstrapReplicaSinkOptions
            {
                ClientKey = "client",
                ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing,
            },
            NullLogger<BootstrapReplicaSinkProvider>.Instance);
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<TestProjection>(
            "sink-a",
            "orders",
            provider,
            projection => new TestContract { Id = projection.Id });
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(TestProjection), "entity-1", 40, new TestProjection { Id = "projection-40" });
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
        ReplicaSinkDeliveryState? failedState = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);
        Assert.NotNull(failedState);
        Assert.Equal(40, failedState.DesiredSourcePosition);
        Assert.Null(failedState.CommittedSourcePosition);

        ReplicaSinkLatestStateProcessor replayProcessor = CreateProcessor([binding], stateStore, sourceAccessor);
        await replayProcessor.FlushAsync<TestProjection>("entity-1", CancellationToken.None);
        ReplicaSinkDeliveryState? replayedState = await stateStore.ReadAsync(GetDeliveryKey(binding, "entity-1"), CancellationToken.None);
        ReplicaTargetInspection inspection = await provider.InspectAsync(binding.Target, CancellationToken.None);

        Assert.NotNull(replayedState);
        Assert.Equal(40, replayedState.CommittedSourcePosition);
        Assert.Equal(ReplicaWriteOutcome.DuplicateIgnored, await GetDuplicateOutcomeAsync(provider, binding));
        Assert.Equal(1, inspection.WriteCount);
        Assert.Equal(40, inspection.LatestSourcePosition);
    }

    /// <summary>
    ///     The UX projection source-state accessor reports deleted when the projection exists at the version but returns null state.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkUxProjectionSourceStateAccessorShouldReturnDeletedWhenProjectionStateIsNullAtAvailablePosition()
    {
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        projectionGrain
            .Setup(grain => grain.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(50));
        projectionGrain
            .Setup(grain => grain.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> grainFactory = new();
        grainFactory.Setup(factory => factory.GetUxProjectionGrain<TestProjection>("entity-1")).Returns(projectionGrain.Object);
        ReplicaSinkUxProjectionSourceStateAccessor accessor = new(grainFactory.Object);

        ReplicaSinkSourceState state = await accessor.ReadAsync(typeof(TestProjection), "entity-1", 50, CancellationToken.None);

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
        projectionGrain
            .Setup(grain => grain.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(51));
        Mock<IUxProjectionGrainFactory> grainFactory = new();
        grainFactory.Setup(factory => factory.GetUxProjectionGrain<TestProjection>("entity-1")).Returns(projectionGrain.Object);
        ReplicaSinkUxProjectionSourceStateAccessor accessor = new(grainFactory.Object);

        ReplicaSinkSourceStateUnavailableException exception = await Assert.ThrowsAsync<ReplicaSinkSourceStateUnavailableException>(async () =>
            await accessor.ReadAsync(typeof(TestProjection), "entity-1", 52, CancellationToken.None));

        Assert.Equal(52, exception.RequestedSourcePosition);
        Assert.Equal(51, exception.LatestAvailableSourcePosition);
    }

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
            new ReplicaWriteRequest(
                binding.Target,
                GetDeliveryKey(binding, "entity-1"),
                40,
                ReplicaWriteMode.LatestState,
                binding.ContractIdentity,
                new TestContract { Id = "projection-40" }),
            CancellationToken.None);
        return result.Outcome;
    }

    private static ReplicaSinkLatestStateProcessor CreateProcessor(
        IReadOnlyList<ReplicaSinkBindingDescriptor> bindings,
        IReplicaSinkDeliveryStateStore stateStore,
        IReplicaSinkSourceStateAccessor sourceStateAccessor,
        TimeProvider? timeProvider = null,
        IReplicaSinkLatestStateProcessorHook? hook = null
    ) =>
        new(
            new TestReplicaSinkProjectionRegistry(bindings),
            stateStore,
            sourceStateAccessor,
            timeProvider ?? TimeProvider.System,
            hook ?? new NullReplicaSinkLatestStateProcessorHook(),
            NullLogger<ReplicaSinkLatestStateProcessor>.Instance);

    private static ReplicaSinkBindingDescriptor CreateMappedBinding<TProjection>(
        string sinkKey,
        string targetName,
        IReplicaSinkProvider provider,
        Func<TProjection, object> map
    )
        where TProjection : class =>
        new(
            new ReplicaSinkBindingIdentity(typeof(TProjection).FullName ?? typeof(TProjection).Name, sinkKey, targetName),
            typeof(TProjection),
            ReplicaWriteMode.LatestState,
            typeof(TestContract),
            "TestApp.Replica.TestContract.V1",
            input => map((TProjection)input),
            false,
            new ReplicaSinkRegistrationDescriptor(
                sinkKey,
                "client",
                provider.Format,
                provider.GetType(),
                ReplicaProvisioningMode.CreateIfMissing),
            provider,
            new ReplicaTargetDescriptor(
                new ReplicaDestinationIdentity("client", targetName),
                ReplicaProvisioningMode.CreateIfMissing));

    private static FakeTimeProvider CreateTimeProvider()
    {
        FakeTimeProvider timeProvider = new();
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
        return timeProvider;
    }

    private static string GetDeliveryKey(
        ReplicaSinkBindingDescriptor binding,
        string entityId
    ) =>
        new ReplicaSinkDeliveryIdentity(binding.Identity, entityId).DeliveryKey;

    private sealed class InMemoryReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
    {
        private ConcurrentDictionary<string, ReplicaSinkDeliveryState> States { get; } = new(StringComparer.Ordinal);

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

    private sealed class RecordingReplicaSinkProvider : IReplicaSinkProvider
    {
        private Func<ReplicaWriteRequest, ReplicaWriteResult> OnWrite { get; }

        public RecordingReplicaSinkProvider(
            Func<ReplicaWriteRequest, ReplicaWriteResult> onWrite
        )
        {
            OnWrite = onWrite;
        }

        public string Format => "test";

        public List<ReplicaWriteRequest> Requests { get; } = [];

        public ValueTask EnsureTargetAsync(
            ReplicaTargetDescriptor target,
            CancellationToken cancellationToken
        ) => ValueTask.CompletedTask;

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
        private IReadOnlyList<ReplicaSinkBindingDescriptor> Bindings { get; }

        public TestReplicaSinkProjectionRegistry(
            IReadOnlyList<ReplicaSinkBindingDescriptor> bindings
        )
        {
            Bindings = bindings;
        }

        public IReadOnlyList<ReplicaSinkBindingDescriptor> GetBindingDescriptors() => Bindings;

        public IReadOnlyList<ReplicaSinkStartupDiagnostic> GetDiagnostics() => [];
    }

    private sealed class TestReplicaSinkSourceStateAccessor : IReplicaSinkSourceStateAccessor
    {
        private Dictionary<(Type ProjectionType, string EntityId, long SourcePosition), ReplicaSinkSourceState> States { get; } = [];

        public List<(Type ProjectionType, string EntityId, long SourcePosition)> ReadRequests { get; } = [];

        public ValueTask<ReplicaSinkSourceState> ReadAsync(
            Type projectionType,
            string entityId,
            long sourcePosition,
            CancellationToken cancellationToken = default
        )
        {
            ReadRequests.Add((projectionType, entityId, sourcePosition));
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
        ) => throw new InvalidOperationException("Synthetic crash after provider write.");
    }
}
