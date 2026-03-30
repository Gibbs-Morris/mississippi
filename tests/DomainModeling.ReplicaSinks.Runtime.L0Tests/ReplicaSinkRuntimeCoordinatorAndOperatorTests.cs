using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
///     Tests replica sink runtime coordinator and runtime-operator behaviors for latest-state delivery.
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

    private static ReplicaSinkDeliveryState CreateLegacyDeadLetterState(
        string deliveryKey,
        ReplicaSinkStoredFailure deadLetter
    )
    {
        ReplicaSinkDeliveryState state =
            (ReplicaSinkDeliveryState)RuntimeHelpers.GetUninitializedObject(typeof(ReplicaSinkDeliveryState));
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.DeliveryKey), deliveryKey);
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.DesiredSourcePosition), null);
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.BootstrapUpperBoundSourcePosition), null);
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.CommittedSourcePosition), null);
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.Retry), null);
        SetDeliveryStateBackingField(state, nameof(ReplicaSinkDeliveryState.DeadLetter), deadLetter);
        return state;
    }

    private static void SetDeliveryStateBackingField(
        ReplicaSinkDeliveryState state,
        string propertyName,
        object? value
    )
    {
        FieldInfo field = typeof(ReplicaSinkDeliveryState).GetField(
                              $"<{propertyName}>k__BackingField",
                              BindingFlags.Instance | BindingFlags.NonPublic) ??
                          throw new InvalidOperationException(
                              $"Replica sink delivery-state backing field '{propertyName}' was not found.");
        field.SetValue(state, value);
    }

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
            Options.Create(new ReplicaSinkRuntimeOptions { MaxDeadLetterPageSize = 1 }),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);
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
            Options.Create(new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);
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

    /// <summary>
    ///     Ensures re-drive clears a legacy dead letter from fresh post-notify state without erasing the newly durable
    ///     desired source position.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldPreserveFreshDesiredPositionWhenReDrivingLegacyNullDesiredDeadLetter()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        string deliveryKey = GetDeliveryKey(binding, "entity-1");
        LegacyDeadLetterReplicaSinkDeliveryStateStore stateStore = new(
            CreateLegacyDeadLetterState(
                deliveryKey,
                new(50, 2, "dead_letter", "Detailed dead-letter.", timeProvider.GetUtcNow())));
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-1", 50, new RuntimeProjection { Id = "projection-50" });
        ReplicaSinkRuntimeCoordinator coordinator = CreateCoordinator([binding], stateStore, sourceAccessor, timeProvider);
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            coordinator,
            new TestReplicaSinkProjectionRegistry([binding]),
            auditSink,
            new ReplicaSinkExecutionHealthManager(timeProvider),
            Options.Create(new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);

        ReplicaSinkDeadLetterReDriveResult result = await runtimeOperator.ReDriveAsync(
            new(new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin), deliveryKey),
            CancellationToken.None);

        ReplicaSinkDeliveryState? updatedState = await stateStore.ReadAsync(deliveryKey, CancellationToken.None);
        int processedCount = await coordinator.ExecuteBatchAsync(CancellationToken.None);
        ReplicaSinkDeliveryState? committedState = await stateStore.ReadAsync(deliveryKey, CancellationToken.None);

        Assert.True(result.WasQueued);
        Assert.Equal("queued", result.Outcome);
        Assert.Equal(50, result.TargetSourcePosition);
        Assert.NotNull(updatedState);
        Assert.Equal(50, updatedState.DesiredSourcePosition);
        Assert.Null(updatedState.DeadLetter);
        Assert.Equal(1, processedCount);
        Assert.NotNull(committedState);
        Assert.Equal(50, committedState.DesiredSourcePosition);
        Assert.Equal(50, committedState.CommittedSourcePosition);
        Assert.Single(provider.Requests);
        Assert.Equal(50, provider.Requests.Single().SourcePosition);
        Assert.Equal(1, auditSink.ReDriveCount);
    }

    /// <summary>
    ///     Ensures re-drive keeps the stored dead letter when the projection type can no longer be resolved.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldKeepDeadLetterStateWhenProjectionTypeCannotBeResolved()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        string deliveryKey = GetDeliveryKey(binding, "entity-1");
        await stateStore.WriteAsync(
            new(
                deliveryKey,
                desiredSourcePosition: 50,
                deadLetter: new(50, 2, "dead_letter", "Detailed dead-letter.", timeProvider.GetUtcNow())));
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            CreateCoordinator([binding], stateStore, new TestReplicaSinkSourceStateAccessor(), timeProvider),
            new TestReplicaSinkProjectionRegistry([]),
            auditSink,
            new ReplicaSinkExecutionHealthManager(timeProvider),
            Options.Create(new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);
        ReplicaSinkDeadLetterReDriveResult result = await runtimeOperator.ReDriveAsync(
            new(new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin), deliveryKey),
            CancellationToken.None);
        ReplicaSinkDeliveryState? updatedState = await stateStore.ReadAsync(deliveryKey, CancellationToken.None);
        Assert.False(result.WasQueued);
        Assert.Equal("projection_type_not_found", result.Outcome);
        Assert.Equal(50, result.TargetSourcePosition);
        Assert.NotNull(updatedState);
        Assert.NotNull(updatedState.DeadLetter);
        Assert.Empty(provider.Requests);
        Assert.Equal(1, auditSink.ReDriveCount);
    }

    /// <summary>
    ///     Ensures re-drive preserves the stored dead letter when coordinator notify fails after projection resolution.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldKeepDeadLetterStateWhenCoordinatorNotifyFails()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        BootstrapReplicaSinkDeliveryStateStore stateStore = new();
        string deliveryKey = GetDeliveryKey(binding, "entity-1");
        await stateStore.WriteAsync(
            new(
                deliveryKey,
                desiredSourcePosition: 50,
                deadLetter: new(50, 2, "dead_letter", "Detailed dead-letter.", timeProvider.GetUtcNow())));
        ThrowingReplicaSinkRuntimeCoordinator coordinator = new();
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            coordinator,
            new TestReplicaSinkProjectionRegistry([binding]),
            auditSink,
            new ReplicaSinkExecutionHealthManager(timeProvider),
            Options.Create(new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runtimeOperator.ReDriveAsync(
                new(new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin), deliveryKey),
                CancellationToken.None));

        ReplicaSinkDeliveryState? updatedState = await stateStore.ReadAsync(deliveryKey, CancellationToken.None);
        Assert.Equal("Synthetic coordinator notify failure.", exception.Message);
        Assert.Equal(1, coordinator.NotifyLiveCallCount);
        Assert.Equal(typeof(RuntimeProjection), coordinator.LastProjectionType);
        Assert.Equal("entity-1", coordinator.LastEntityId);
        Assert.Equal(50, coordinator.LastDesiredSourcePosition);
        Assert.NotNull(updatedState);
        Assert.NotNull(updatedState.DeadLetter);
        Assert.Equal(0, auditSink.ReDriveCount);
    }

    /// <summary>
    ///     Ensures re-drive quarantines the sink and preserves the dead letter when dead-letter clearing fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeOperatorShouldQuarantineSinkWhenDeadLetterClearFails()
    {
        FakeTimeProvider timeProvider = CreateTimeProvider();
        RecordingReplicaSinkProvider provider = new(request => new(
            ReplicaWriteOutcome.Applied,
            request.Target.DestinationIdentity,
            request.SourcePosition));
        ReplicaSinkBindingDescriptor binding = CreateMappedBinding<RuntimeProjection>("sink-a", "orders", provider);
        DeadLetterClearFailingReplicaSinkDeliveryStateStore stateStore = new();
        string deliveryKey = GetDeliveryKey(binding, "entity-1");
        await stateStore.SeedAsync(
            new(
                deliveryKey,
                desiredSourcePosition: 50,
                deadLetter: new(50, 2, "dead_letter", "Detailed dead-letter.", timeProvider.GetUtcNow())));
        TestReplicaSinkSourceStateAccessor sourceAccessor = new();
        sourceAccessor.SetValue(typeof(RuntimeProjection), "entity-1", 50, new RuntimeProjection { Id = "projection-50" });
        ReplicaSinkExecutionHealthManager healthManager = new(timeProvider);
        RecordingReplicaSinkOperatorAuditSink auditSink = new();
        ReplicaSinkRuntimeOperator runtimeOperator = new(
            stateStore,
            CreateCoordinator([binding], stateStore, sourceAccessor, timeProvider, healthManager: healthManager),
            new TestReplicaSinkProjectionRegistry([binding]),
            auditSink,
            healthManager,
            Options.Create(new ReplicaSinkRuntimeOptions()),
            NullLogger<ReplicaSinkRuntimeOperator>.Instance);
        ReplicaSinkDeadLetterReDriveResult result = await runtimeOperator.ReDriveAsync(
            new(new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin), deliveryKey),
            CancellationToken.None);
        ReplicaSinkDeliveryState? updatedState = await stateStore.ReadAsync(deliveryKey, CancellationToken.None);
        ReplicaSinkExecutionBlock? block = healthManager.GetCurrentBlock("sink-a");
        Assert.False(result.WasQueued);
        Assert.Equal("dead_letter_store_quarantined", result.Outcome);
        Assert.Equal(50, result.TargetSourcePosition);
        Assert.NotNull(updatedState);
        Assert.NotNull(updatedState.DeadLetter);
        Assert.NotNull(block);
        Assert.Equal(ReplicaSinkExecutionBlockKind.Quarantined, block.Kind);
        Assert.Empty(provider.Requests);
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

    private sealed class DeadLetterClearFailingReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
    {
        private BootstrapReplicaSinkDeliveryStateStore InnerStore { get; } = new();

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

        public Task SeedAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        ) =>
            InnerStore.WriteAsync(state, cancellationToken);

        public Task WriteAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        )
        {
            if (state.DeadLetter is null)
            {
                throw new InvalidOperationException("Synthetic dead-letter clear persistence failure.");
            }

            return InnerStore.WriteAsync(state, cancellationToken);
        }
    }

    private sealed class LegacyDeadLetterReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
    {
        public LegacyDeadLetterReplicaSinkDeliveryStateStore(
            ReplicaSinkDeliveryState initialState
        ) =>
            States[initialState.DeliveryKey] = initialState;

        private Dictionary<string, ReplicaSinkDeliveryState> States { get; } = new(StringComparer.Ordinal);

        public Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
            int pageSize,
            string? continuationToken = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<ReplicaSinkDeliveryState> items = States.Values
                .Where(static state => state.DeadLetter is not null)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult(new ReplicaSinkDeliveryStatePage(items, continuationToken: null));
        }

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
            DateTimeOffset dueAtOrBeforeUtc,
            int maxCount,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<ReplicaSinkDeliveryState> items = States.Values
                .Where(state => state.Retry?.NextRetryAtUtc is DateTimeOffset nextRetryAtUtc && nextRetryAtUtc <= dueAtOrBeforeUtc)
                .Take(maxCount)
                .ToArray();
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
            ArgumentNullException.ThrowIfNull(state);
            cancellationToken.ThrowIfCancellationRequested();
            States[state.DeliveryKey] = state;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingReplicaSinkRuntimeCoordinator : IReplicaSinkRuntimeCoordinator
    {
        public string? LastEntityId { get; private set; }

        public long? LastDesiredSourcePosition { get; private set; }

        public Type? LastProjectionType { get; private set; }

        public int NotifyLiveCallCount { get; private set; }

        public Task<int> ExecuteBatchAsync(
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(0);

        public Task NotifyLiveAsync<TProjection>(
            string entityId,
            long desiredSourcePosition,
            CancellationToken cancellationToken = default
        )
            where TProjection : class =>
            NotifyLiveAsync(typeof(TProjection), entityId, desiredSourcePosition, cancellationToken);

        public Task NotifyLiveAsync(
            Type projectionType,
            string entityId,
            long desiredSourcePosition,
            CancellationToken cancellationToken = default
        )
        {
            LastProjectionType = projectionType;
            LastEntityId = entityId;
            LastDesiredSourcePosition = desiredSourcePosition;
            NotifyLiveCallCount++;
            return Task.FromException(new InvalidOperationException("Synthetic coordinator notify failure."));
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

        public Task RegisterBootstrapAsync(
            Type projectionType,
            string entityId,
            string sinkKey,
            string targetName,
            long bootstrapUpperBoundSourcePosition,
            long desiredSourcePosition,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;
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
        private SemaphoreSlim FirstWriteStartedSignal { get; } = new(0, 1);

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

        public Task WaitForFirstWriteAsync() => FirstWriteStartedSignal.WaitAsync();

        public async ValueTask<ReplicaWriteResult> WriteAsync(
            ReplicaWriteRequest request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                _ = FirstWriteStartedSignal.Release();
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
