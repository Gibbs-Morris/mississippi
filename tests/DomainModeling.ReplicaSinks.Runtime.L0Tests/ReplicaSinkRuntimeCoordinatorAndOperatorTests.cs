using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests the Increment 03b runtime coordinator and runtime-operator behaviors.
/// </summary>
public sealed class ReplicaSinkRuntimeCoordinatorAndOperatorTests
{
    private static ReplicaSinkBindingDescriptor CreateMappedBinding<TProjection>(
        string sinkKey,
        string targetName,
        IReplicaSinkProvider provider
    )
        where TProjection : class, IRuntimeTestProjection =>
        new(
            new(typeof(TProjection).FullName ?? typeof(TProjection).Name, sinkKey, targetName),
            typeof(TProjection),
            ReplicaWriteMode.LatestState,
            typeof(RuntimeReplicaContract),
            "TestApp.Replica.RuntimeReplicaContract.V1",
            input =>
            {
                IRuntimeTestProjection projection = (IRuntimeTestProjection)input;
                return new RuntimeReplicaContract
                {
                    Id = projection.Id,
                };
            },
            false,
            new(sinkKey, "client", provider.Format, provider.GetType(), ReplicaProvisioningMode.CreateIfMissing),
            provider,
            new(new("client", targetName), ReplicaProvisioningMode.CreateIfMissing));

    private static ReplicaSinkRuntimeCoordinator CreateCoordinator(
        IReadOnlyList<ReplicaSinkBindingDescriptor> bindings,
        IReplicaSinkDeliveryStateStore stateStore,
        TestReplicaSinkSourceStateAccessor sourceStateAccessor,
        FakeTimeProvider timeProvider,
        ReplicaSinkRuntimeOptions? runtimeOptions = null,
        ReplicaSinkExecutionHealthManager? healthManager = null
    )
    {
        ReplicaSinkExecutionHealthManager effectiveHealthManager = healthManager ?? new(timeProvider);
        TestReplicaSinkProjectionRegistry registry = new(bindings);
        return new(
            new ReplicaSinkLatestStateProcessor(
                registry,
                stateStore,
                sourceStateAccessor,
                timeProvider,
                new NullReplicaSinkLatestStateProcessorHook(),
                effectiveHealthManager,
                Options.Create(runtimeOptions ?? new ReplicaSinkRuntimeOptions()),
                NullLogger<ReplicaSinkLatestStateProcessor>.Instance),
            registry,
            stateStore,
            effectiveHealthManager,
            timeProvider,
            Options.Create(runtimeOptions ?? new ReplicaSinkRuntimeOptions()));
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        FakeTimeProvider timeProvider = new();
        timeProvider.SetUtcNow(new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));
        return timeProvider;
    }

    private static string GetDeliveryKey(
        ReplicaSinkBindingDescriptor binding,
        string entityId
    ) =>
        new ReplicaSinkDeliveryIdentity(binding.Identity, entityId).DeliveryKey;

    /// <summary>
    ///     Ensures bounded execution prefers queued bootstrap work before later live work.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeCoordinatorShouldProcessBootstrapBeforeLiveWorkWhenBatchIsBounded()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "bootstrap-entity", 10, new RuntimeProjection { Id = "projection-10" });
        sourceAccessor.SetValue(typeof(RuntimeProjection), "live-entity", 20, new RuntimeProjection { Id = "projection-20" });
        ReplicaSinkRuntimeCoordinator coordinator = CreateCoordinator(
            [binding],
            stateStore,
            sourceAccessor,
            timeProvider,
            new ReplicaSinkRuntimeOptions
            {
                MaxExecutionBatchSize = 1,
                MaxExecutionBatchSizePerSink = 1,
            });
        await coordinator.RegisterBootstrapAsync<RuntimeProjection>(
            "bootstrap-entity",
            "sink-a",
            "orders",
            10,
            12,
            CancellationToken.None);
        await coordinator.NotifyLiveAsync<RuntimeProjection>("live-entity", 20, CancellationToken.None);
        int firstBatchCount = await coordinator.ExecuteBatchAsync(CancellationToken.None);
        int secondBatchCount = await coordinator.ExecuteBatchAsync(CancellationToken.None);
        Assert.Equal(1, firstBatchCount);
        Assert.Equal(1, secondBatchCount);
        Assert.Equal([10L, 20L], provider.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     Ensures retry hydration respects the configured per-sink retry selection budget.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeCoordinatorShouldHonorPerSinkRetrySelectionBudget()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider providerA = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        RecordingReplicaSinkProvider providerB = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor bindingA = CreateMappedBinding<RuntimeProjection>("sink-a", "orders-a", providerA);
        ReplicaSinkBindingDescriptor bindingB = CreateMappedBinding<SecondaryRuntimeProjection>("sink-b", "orders-b", providerB);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-a1", 30, new RuntimeProjection { Id = "projection-a1" });
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-a2", 31, new RuntimeProjection { Id = "projection-a2" });
        sourceAccessor.SetValue(
            typeof(SecondaryRuntimeProjection),
            "entity-b1",
            32,
            new SecondaryRuntimeProjection { Id = "projection-b1" });
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(bindingA, "entity-a1"),
                desiredSourcePosition: 30,
                retry: new(30, 1, "retry_a1", "Retry A1.", timeProvider.GetUtcNow(), timeProvider.GetUtcNow())));
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(bindingA, "entity-a2"),
                desiredSourcePosition: 31,
                retry: new(31, 1, "retry_a2", "Retry A2.", timeProvider.GetUtcNow(), timeProvider.GetUtcNow())));
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(bindingB, "entity-b1"),
                desiredSourcePosition: 32,
                retry: new(32, 1, "retry_b1", "Retry B1.", timeProvider.GetUtcNow(), timeProvider.GetUtcNow())));
        ReplicaSinkRuntimeCoordinator coordinator = CreateCoordinator(
            [bindingA, bindingB],
            stateStore,
            sourceAccessor,
            timeProvider,
            new ReplicaSinkRuntimeOptions
            {
                MaxExecutionBatchSize = 3,
                MaxExecutionBatchSizePerSink = 3,
                MaxRetrySelectionsPerBatch = 3,
                MaxRetrySelectionsPerSink = 1,
            });
        int processedCount = await coordinator.ExecuteBatchAsync(CancellationToken.None);
        Assert.Equal(2, processedCount);
        Assert.Single(providerA.Requests);
        Assert.Single(providerB.Requests);
        Assert.Equal([30L], providerA.Requests.Select(static request => request.SourcePosition).ToArray());
        Assert.Equal([32L], providerB.Requests.Select(static request => request.SourcePosition).ToArray());
    }

    /// <summary>
    ///     Ensures concurrent batch executions serialize through the coordinator's single-flight gate.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeCoordinatorShouldSerializeConcurrentExecuteBatchCallsForSingleFlight()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        BlockingReplicaSinkProvider provider = new();
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-1", 60, new RuntimeProjection { Id = "projection-60" });
        ReplicaSinkRuntimeCoordinator coordinator = CreateCoordinator([binding], stateStore, sourceAccessor, timeProvider);
        await coordinator.NotifyLiveAsync<RuntimeProjection>("entity-1", 60, CancellationToken.None);
        Task<int> firstBatchTask = coordinator.ExecuteBatchAsync(CancellationToken.None);
        await provider.WaitForFirstWriteAsync();
        Task<int> secondBatchTask = coordinator.ExecuteBatchAsync(CancellationToken.None);
        provider.ReleaseFirstWrite();
        int firstBatchCount = await firstBatchTask;
        int secondBatchCount = await secondBatchTask;
        Assert.Equal(1, firstBatchCount);
        Assert.Equal(0, secondBatchCount);
        Assert.Single(provider.Requests);
        Assert.Equal(60, provider.Requests.Single().SourcePosition);
    }

    /// <summary>
    ///     Ensures summary-level reads page dead letters and redact failure summaries.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldPageAndRedactDeadLettersForSummaryAccess()
    {
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(binding, "entity-1"),
                desiredSourcePosition: 40,
                deadLetter: new(40, 1, "dead_letter_1", "Detailed dead-letter 1.", new(2026, 3, 30, 12, 1, 0, TimeSpan.Zero))));
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(binding, "entity-2"),
                desiredSourcePosition: 41,
                deadLetter: new(41, 1, "dead_letter_2", "Detailed dead-letter 2.", new(2026, 3, 30, 12, 2, 0, TimeSpan.Zero))));
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            CreateCoordinator([binding], stateStore, new TestReplicaSinkSourceStateAccessor(), CreateTimeProvider()),
            new TestReplicaSinkProjectionRegistry([binding]),
            auditSink,
            new ReplicaSinkExecutionHealthManager(TimeProvider.System),
            Options.Create(new ReplicaSinkRuntimeOptions { MaxDeadLetterPageSize = 1 }));
        ReplicaSinkDeadLetterPage firstPage = await runtimeOperator.ReadDeadLettersAsync(
            new(
                new("operator-a", ReplicaSinkOperatorAccessLevel.Summary),
                pageSize: 5,
                includeFailureSummary: true),
            CancellationToken.None);
        ReplicaSinkDeadLetterPage secondPage = await runtimeOperator.ReadDeadLettersAsync(
            new(
                new("operator-a", ReplicaSinkOperatorAccessLevel.Summary),
                pageSize: 5,
                continuationToken: firstPage.ContinuationToken,
                includeFailureSummary: true),
            CancellationToken.None);
        Assert.Single(firstPage.Items);
        Assert.Null(firstPage.Items.Single().FailureSummary);
        Assert.True(firstPage.Items.Single().IsFailureSummaryRedacted);
        Assert.NotNull(firstPage.ContinuationToken);
        Assert.Single(secondPage.Items);
        Assert.Equal(2, auditSink.ReadCount);
        Assert.True(auditSink.RedactedFailureSummary);
    }

    /// <summary>
    ///     Ensures admin re-drive clears the stored dead letter and re-queues execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldClearDeadLetterAndQueueControlledReDrive()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-1", 50, new RuntimeProjection { Id = "projection-50" });
        await stateStore.WriteAsync(
            new(
                GetDeliveryKey(binding, "entity-1"),
                desiredSourcePosition: 50,
                deadLetter: new(50, 2, "dead_letter", "Detailed dead-letter.", timeProvider.GetUtcNow())));
        ReplicaSinkRuntimeCoordinator coordinator = CreateCoordinator([binding], stateStore, sourceAccessor, timeProvider);
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            coordinator,
            new TestReplicaSinkProjectionRegistry([binding]),
            auditSink,
            new ReplicaSinkExecutionHealthManager(timeProvider),
            Options.Create(new ReplicaSinkRuntimeOptions()));
        ReplicaSinkDeadLetterReDriveResult result = await runtimeOperator.ReDriveAsync(
            new(new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin), GetDeliveryKey(binding, "entity-1")),
            CancellationToken.None);
        ReplicaSinkDeliveryState? updatedState = await stateStore.ReadAsync(
            GetDeliveryKey(binding, "entity-1"),
            CancellationToken.None);
        int processedCount = await coordinator.ExecuteBatchAsync(CancellationToken.None);
        Assert.True(result.WasQueued);
        Assert.Equal("queued", result.Outcome);
        Assert.NotNull(updatedState);
        Assert.Null(updatedState.DeadLetter);
        Assert.Equal(1, processedCount);
        Assert.Single(provider.Requests);
        Assert.Equal(50, provider.Requests.Single().SourcePosition);
        Assert.Equal(1, auditSink.ReDriveCount);
    }

    private sealed class RecordingReplicaSinkOperatorAuditSink : IReplicaSinkOperatorAuditSink
    {
        public int ReadCount { get; private set; }

        public int ReDriveCount { get; private set; }

        public bool RedactedFailureSummary { get; private set; }

        public Task RecordDeadLetterReadAsync(
            ReplicaSinkDeadLetterQuery query,
            int effectivePageSize,
            int resultCount,
            bool includedFailureSummary,
            bool redactedFailureSummary,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            RedactedFailureSummary = redactedFailureSummary;
            return Task.CompletedTask;
        }

        public Task RecordReDriveAsync(
            ReplicaSinkDeadLetterReDriveRequest request,
            ReplicaSinkDeadLetterReDriveResult result,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReDriveCount++;
            return Task.CompletedTask;
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

    private sealed class BlockingReplicaSinkProvider : IReplicaSinkProvider
    {
        private TaskCompletionSource<bool> FirstWriteStartedSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private TaskCompletionSource<bool> ReleaseFirstWriteSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Format => "test";

        public List<ReplicaWriteRequest> Requests { get; } = [];

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

        public void ReleaseFirstWrite() => ReleaseFirstWriteSource.TrySetResult(true);

        public Task WaitForFirstWriteAsync() => FirstWriteStartedSource.Task;

        public async ValueTask<ReplicaWriteResult> WriteAsync(
            ReplicaWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                FirstWriteStartedSource.TrySetResult(true);
                await ReleaseFirstWriteSource.Task.WaitAsync(cancellationToken);
            }

            return new(ReplicaWriteOutcome.Applied, request.Target.DestinationIdentity, request.SourcePosition);
        }
    }

    /// <summary>
    ///     Shared test projection contract used by the generic mapping helper.
    /// </summary>
    private interface IRuntimeTestProjection
    {
        /// <summary>
        ///     Gets the stable identifier copied into the replica contract.
        /// </summary>
        string Id { get; }
    }

    private sealed class RuntimeProjection : IRuntimeTestProjection
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class RuntimeReplicaContract
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class SecondaryRuntimeProjection : IRuntimeTestProjection
    {
        public string Id { get; set; } = string.Empty;
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
        private Dictionary<(Type ProjectionType, string EntityId, long SourcePosition), ReplicaSinkSourceState> States { get; } = [];

        public ValueTask<ReplicaSinkSourceState> ReadAsync(
            Type projectionType,
            string entityId,
            long sourcePosition,
            CancellationToken cancellationToken = default
        ) =>
            ValueTask.FromResult(States[(projectionType, entityId, sourcePosition)]);

        public void SetValue(
            Type projectionType,
            string entityId,
            long sourcePosition,
            object value
        ) =>
            States[(projectionType, entityId, sourcePosition)] = ReplicaSinkSourceState.FromValue(sourcePosition, value);
    }
}
