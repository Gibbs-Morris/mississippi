using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs.Models;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace MississippiTests.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Verifies ownership during an append with an independently modeled finite Blob lease.
/// </summary>
public sealed class EventBrookWriterLeaseLifetimeTests
{
    private const string LeaseA = "00000000-0000-0000-0000-000000000001";

    private const string LeaseB = "00000000-0000-0000-0000-000000000002";

    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly ImmutableArray<long> ExpectedPositions = [1, 2];

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBrookWriterLeaseLifetimeTests" /> class.
    /// </summary>
    /// <param name="output">The retained test output.</param>
    public EventBrookWriterLeaseLifetimeTests(
        ITestOutputHelper output
    ) =>
        Output = output;

    private ITestOutputHelper Output { get; }

    private static ItemResponse<T> CreateItemResponse<T>(
        T value
    )
    {
        Mock<ItemResponse<T>> response = new();
        response.SetupGet(r => r.Resource).Returns(value);
        return response.Object;
    }

    private static EventBrookWriter CreateWriter(
        CosmosRepository repository,
        IBlobLeaseClient leaseClient,
        TimeProvider clock,
        IRetryPolicy retry
    )
    {
        BrookStorageOptions options = new();
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(async (string key, TimeSpan duration, CancellationToken token) =>
            {
                Azure.Response<BlobLease> acquired = await leaseClient.AcquireAsync(duration, cancellationToken: token);
                return new BlobDistributedLock(
                    leaseClient,
                    acquired.Value.LeaseId,
                    options.LeaseRenewalThresholdSeconds,
                    options.LeaseDurationSeconds,
                    key,
                    Stopwatch.StartNew(),
                    clock);
            });
        BrookRecoveryService recovery = new(
            repository,
            retry,
            locks.Object,
            Options.Create(options),
            NullLogger<BrookRecoveryService>.Instance);
        return new(
            repository,
            locks.Object,
            new BatchSizeEstimator(),
            retry,
            Options.Create(options),
            new EventToStorageMapper(),
            recovery,
            NullLogger<EventBrookWriter>.Instance,
            clock);
    }

    private static async Task RenewUntilStoppedAsync(
        BlobDistributedLock owned,
        TimeProvider clock,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(20), clock, token);
            await owned.RenewAsync(token);
        }

        token.ThrowIfCancellationRequested();
    }

    private static async Task RunControlledTimerOperationAsync(
        CadenceTimeProvider clock,
        TaskCompletionSource entered,
        Task release,
        bool fail,
        CancellationToken token
    )
    {
        await Task.Delay(TimeSpan.FromSeconds(1), clock, token);
        entered.TrySetResult();
        await release.WaitAsync(token);
        if (fail)
        {
            throw new InvalidOperationException("Injected operation failure");
        }
    }

    private sealed class CadenceTimeProvider : TimeProvider
    {
        private readonly FakeTimeProvider clock = new(BaseTime);

        private readonly object timerState = new();

        private long callbacks;

        private TaskCompletionSource nextTimerRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int registrations;

        public override TimeZoneInfo LocalTimeZone => clock.LocalTimeZone;

        public int TimerRegistrations
        {
            get
            {
                lock (timerState)
                {
                    return registrations;
                }
            }
        }

        public override long TimestampFrequency => clock.TimestampFrequency;

        public ConcurrentQueue<string> Trace { get; } = new();

        public async Task AdvanceAndDrainAsync(
            TimeSpan amount,
            CancellationToken token,
            Task? operationCompletion = null
        )
        {
            Task registration;
            long before;
            lock (timerState)
            {
                registration = nextTimerRegistered.Task;
                before = callbacks;
            }

            clock.Advance(amount);
            bool fired;
            lock (timerState)
            {
                fired = callbacks > before;
            }

            if (fired)
            {
                if (operationCompletion == null)
                {
                    await registration.WaitAsync(token);
                }
                else
                {
                    await Task.WhenAny(registration, operationCompletion).WaitAsync(token);
                }
            }

            Trace.Enqueue(
                $"advance {amount.TotalSeconds}s to {GetUtcNow():O}; fired={fired}; rearm observed={registration.IsCompleted}; operation completed={operationCompletion?.IsCompleted}");
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            ITimer timer = clock.CreateTimer(
                delivered =>
                {
                    lock (timerState)
                    {
                        callbacks++;
                    }

                    callback(delivered);
                },
                state,
                dueTime,
                period);
            TaskCompletionSource registered;
            lock (timerState)
            {
                registrations++;
                registered = nextTimerRegistered;
                nextTimerRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Trace.Enqueue($"register delay {dueTime.TotalSeconds}s at {GetUtcNow():O}");
            registered.TrySetResult();
            return timer;
        }

        public override long GetTimestamp() => clock.GetTimestamp();

        public override DateTimeOffset GetUtcNow() => clock.GetUtcNow();
    }

    private sealed class FiniteLeaseBackend
    {
        private readonly object state = new();

        private TimeSpan duration;

        private DateTimeOffset expiry;

        private string? retainedLeaseId;

        public FiniteLeaseBackend(
            TimeProvider clock
        ) =>
            Clock = clock;

        public string? ActiveLeaseId
        {
            get
            {
                lock (state)
                {
                    return Clock.GetUtcNow() < expiry ? retainedLeaseId : null;
                }
            }
        }

        public ConcurrentQueue<string> Trace { get; } = new();

        private TimeProvider Clock { get; }

        public IBlobLeaseClient CreateClient(
            string leaseId,
            Task? releaseBarrier = null,
            Action? releaseStarted = null,
            Task? renewalBarrier = null,
            Action? renewalStarted = null,
            CancellationToken cleanupToken = default
        )
        {
            Mock<IBlobLeaseClient> client = new(MockBehavior.Strict);
            client.SetupGet(l => l.LeaseId).Returns(leaseId);
            client.Setup(l => l.AcquireAsync(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<RequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((TimeSpan requestedDuration, RequestConditions? _, CancellationToken token) =>
                {
                    token.ThrowIfCancellationRequested();
                    lock (state)
                    {
                        if ((requestedDuration < TimeSpan.FromSeconds(15)) ||
                            (requestedDuration > TimeSpan.FromSeconds(60)))
                        {
                            throw new ArgumentOutOfRangeException(nameof(requestedDuration));
                        }

                        if ((retainedLeaseId != null) && (Clock.GetUtcNow() < expiry))
                        {
                            throw new RequestFailedException(409, "Lease already present", "LeaseAlreadyPresent", null);
                        }

                        retainedLeaseId = leaseId;
                        duration = requestedDuration;
                        expiry = Clock.GetUtcNow() + duration;
                        Trace.Enqueue($"acquire {leaseId} at {Clock.GetUtcNow():O}; expires {expiry:O}");
                        return Task.FromResult(CreateLeaseResponse(leaseId));
                    }
                });
            client.Setup(l => l.RenewAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
                .Returns(async (RequestConditions? _, CancellationToken token) =>
                {
                    token.ThrowIfCancellationRequested();
                    renewalStarted?.Invoke();
                    if (renewalBarrier != null)
                    {
                        await renewalBarrier.WaitAsync(token);
                    }

                    token.ThrowIfCancellationRequested();
                    lock (state)
                    {
                        RequireRetainedId(leaseId);
                        expiry = Clock.GetUtcNow() + duration;
                        Trace.Enqueue($"renew {leaseId} at {Clock.GetUtcNow():O}; expires {expiry:O}");
                        return CreateLeaseResponse(leaseId);
                    }
                });
            client.Setup(l => l.ReleaseAsync(It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()))
                .Returns(async (RequestConditions? _, CancellationToken token) =>
                {
                    releaseStarted?.Invoke();
                    if (releaseBarrier != null)
                    {
                        await releaseBarrier.WaitAsync(cleanupToken);
                    }

                    token.ThrowIfCancellationRequested();
                    lock (state)
                    {
                        RequireRetainedId(leaseId);
                        Trace.Enqueue($"release {leaseId} at {Clock.GetUtcNow():O}");
                        retainedLeaseId = null;
                    }

                    return Mock.Of<Azure.Response<ReleasedObjectInfo>>();
                });
            return client.Object;
        }

        private Azure.Response<BlobLease> CreateLeaseResponse(
            string leaseId
        ) =>
            Response.FromValue(
                BlobsModelFactory.BlobLease(new("\"etag\""), Clock.GetUtcNow(), leaseId),
                Mock.Of<Response>());

        private void RequireRetainedId(
            string leaseId
        )
        {
            if (retainedLeaseId != leaseId)
            {
                Trace.Enqueue($"reject stale {leaseId} at {Clock.GetUtcNow():O}; retained {retainedLeaseId}");
                throw new RequestFailedException(409, "Lease ID mismatch", "LeaseIdMismatchWithLeaseOperation", null);
            }
        }
    }

    private sealed class ImmediateRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return operation();
        }
    }

    private sealed record Mutation(string Operation, string? Owner, long Position, DateTimeOffset Time);

    /// <summary>
    ///     A slow event create must not allow later event writes and cursor publication under another owner's lease.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task AppendRetainsLeaseThroughSlowStorageWithCadenceAsync()
    {
        using CancellationTokenSource deadline =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        CadenceTimeProvider clock = new();
        FiniteLeaseBackend leases = new(clock);
        TaskCompletionSource firstCreateEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resumeFirstCreate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishReleaseB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<Mutation> mutations = new();
        object documents = new();
        BrookKey key = new("type", "lease-lifetime");
        CursorDocument cursor = new()
        {
            Id = "cursor",
            BrookPartitionKey = key.ToString(),
            Position = 0,
        };
        CursorDocument? pending = null;
        int pendingConflicts = 0;
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, PartitionKey _, ItemRequestOptions? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                lock (documents)
                {
                    return Task.FromResult(CreateItemResponse(cursor));
                }
            });
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((CursorDocument value, PartitionKey? _, ItemRequestOptions? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                lock (documents)
                {
                    if (pending != null)
                    {
                        pendingConflicts++;
                        throw new CosmosException(
                            "Pending document already exists",
                            HttpStatusCode.Conflict,
                            0,
                            "pending-control",
                            0);
                    }

                    pending = value;
                    return Task.FromResult(CreateItemResponse(value));
                }
            });
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (EventDocument value, PartitionKey? _, ItemRequestOptions? _, CancellationToken token) =>
            {
                mutations.Enqueue(new("event-create", leases.ActiveLeaseId, value.Position, clock.GetUtcNow()));
                if (value.Position == 1)
                {
                    firstCreateEntered.TrySetResult();
                    await resumeFirstCreate.Task.WaitAsync(token);
                }

                token.ThrowIfCancellationRequested();
                return CreateItemResponse(value);
            });
        container.Setup(c => c.UpsertItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((CursorDocument value, PartitionKey? _, ItemRequestOptions? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                mutations.Enqueue(new("cursor-upsert", leases.ActiveLeaseId, value.Position, clock.GetUtcNow()));
                lock (documents)
                {
                    cursor = value;
                }

                return Task.FromResult(CreateItemResponse(value));
            });
        container.Setup(c => c.DeleteItemAsync<CursorDocument>(
                "cursor-pending",
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, PartitionKey _, ItemRequestOptions? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                lock (documents)
                {
                    pending = null;
                }

                return Task.FromResult(CreateItemResponse(new CursorDocument()));
            });
        ImmediateRetryPolicy retry = new();
        CosmosRepository repository = new(
            container.Object,
            retry,
            new CursorDocumentToStorageMapper(),
            new EventDocumentToStorageMapper());
        EventBrookWriter writerA = CreateWriter(
            repository,
            leases.CreateClient(LeaseA, cleanupToken: deadline.Token),
            clock,
            retry);
        EventBrookWriter writerB = CreateWriter(
            repository,
            leases.CreateClient(
                LeaseB,
                finishReleaseB.Task,
                () => releaseBEntered.TrySetResult(),
                cleanupToken: deadline.Token),
            clock,
            retry);
        BrookEvent[] events =
        [
            new()
            {
                Id = "first",
                EventType = "test-event",
                Time = BaseTime,
            },
            new()
            {
                Id = "second",
                EventType = "test-event",
                Time = BaseTime,
            },
        ];
        Task<BrookPosition>? appendA = null;
        Task<BrookPosition>? appendB = null;
        try
        {
            appendA = writerA.AppendEventsAsync(key, events, new(0), deadline.Token);
            await firstCreateEntered.Task.WaitAsync(deadline.Token);
            Assert.Equal(LeaseA, leases.ActiveLeaseId);
            for (int second = 0; second < 61; second++)
            {
                await clock.AdvanceAndDrainAsync(TimeSpan.FromSeconds(1), deadline.Token, appendA);
            }

            string? ownerAt61 = leases.ActiveLeaseId;
            appendB = writerB.AppendEventsAsync(key, events, new(0), deadline.Token);
            if (leases.ActiveLeaseId == LeaseB)
            {
                await releaseBEntered.Task.WaitAsync(deadline.Token);
            }

            resumeFirstCreate.TrySetResult();
            BrookPosition resultA = await appendA;
            finishReleaseB.TrySetResult();
            Exception? competingFailure = null;
            try
            {
                await appendB;
            }
            catch (CosmosException exception)
            {
                competingFailure = exception;
            }
            catch (RequestFailedException exception)
            {
                competingFailure = exception;
            }

            Mutation[] observed = mutations.ToArray();
            Output.WriteLine(
                JsonSerializer.Serialize(
                    new
                    {
                        LeaseTrace = leases.Trace.ToArray(),
                        Mutations = observed,
                        PendingConflicts = pendingConflicts,
                        CompetingFailure = competingFailure?.GetType().Name,
                        OwnerAt61 = ownerAt61,
                        clock.TimerRegistrations,
                        ClockTrace = clock.Trace.ToArray(),
                        ResultA = resultA.Value,
                    }));
            if (competingFailure is CosmosException pendingConflict)
            {
                Assert.Equal(HttpStatusCode.Conflict, pendingConflict.StatusCode);
                Assert.Equal(1, pendingConflicts);
            }
            else
            {
                RequestFailedException activeLeaseConflict = Assert.IsType<RequestFailedException>(competingFailure);
                Assert.Equal(409, activeLeaseConflict.Status);
                Assert.Equal(0, pendingConflicts);
            }

            Assert.Equal(2, resultA.Value);
            Assert.Equal(ExpectedPositions, observed.Where(m => m.Operation == "event-create").Select(m => m.Position));
            Assert.Single(observed, m => m.Operation == "cursor-upsert");
            Assert.All(observed, mutation => Assert.Equal(LeaseA, mutation.Owner));
            Assert.Equal(LeaseA, ownerAt61);
        }
        finally
        {
            resumeFirstCreate.TrySetResult();
            finishReleaseB.TrySetResult();
            foreach (Task<BrookPosition> append in new[] { appendA, appendB }.OfType<Task<BrookPosition>>())
            {
                try
                {
                    await append;
                }
                catch (CosmosException)
                {
                    // Await the expected pending conflict without replacing an earlier assertion.
                }
                catch (RequestFailedException)
                {
                    // The competing acquire is expected to fail while A keeps its lease.
                }
                catch (OperationCanceledException)
                {
                    // The linked deadline releases a blocked operation before cleanup completes.
                }
            }
        }
    }

    /// <summary>
    ///     The clock observes a canceled or faulted operation instead of waiting for a nonexistent rearm.
    /// </summary>
    /// <param name="fail">Whether the controlled operation faults rather than being canceled.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CadenceAcknowledgesCanceledOrFaultedOperationAsync(
        bool fail
    )
    {
        using CancellationTokenSource deadline =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        CadenceTimeProvider clock = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task operation = RunControlledTimerOperationAsync(clock, entered, release.Task, fail, stop.Token);
        try
        {
            Task advancing = clock.AdvanceAndDrainAsync(TimeSpan.FromSeconds(1), deadline.Token, operation);
            await entered.Task.WaitAsync(deadline.Token);
            Assert.False(advancing.IsCompleted);
            if (fail)
            {
                release.TrySetResult();
            }
            else
            {
                await stop.CancelAsync();
            }

            await advancing;
            Assert.True(operation.IsCompleted);
            Exception? failure = null;
            try
            {
                await operation;
            }
            catch (InvalidOperationException exception)
            {
                failure = exception;
            }
            catch (OperationCanceledException exception)
            {
                failure = exception;
            }

            if (fail)
            {
                Assert.IsType<InvalidOperationException>(failure);
            }
            else
            {
                Assert.IsType<OperationCanceledException>(failure, false);
            }

            Assert.Equal(1, clock.TimerRegistrations);
            Output.WriteLine(
                JsonSerializer.Serialize(
                    new
                    {
                        OperationAcknowledged = true,
                        OperationStatus = operation.Status,
                        clock.TimerRegistrations,
                        ClockTrace = clock.Trace.ToArray(),
                    }));
        }
        finally
        {
            release.TrySetResult();
            await stop.CancelAsync();
            try
            {
                await operation;
            }
            catch (InvalidOperationException)
            {
                // The control owns and observes the injected failure.
            }
            catch (OperationCanceledException)
            {
                // The control owns and observes cancellation.
            }
        }
    }

    /// <summary>
    ///     A fired renewal delay must finish its asynchronous renewal before the next clock step.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CadenceWaitsForObservedRenewalAndRearmAsync()
    {
        using CancellationTokenSource deadline =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        CadenceTimeProvider clock = new();
        FiniteLeaseBackend backend = new(clock);
        TaskCompletionSource renewalEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishRenewal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IBlobLeaseClient client = backend.CreateClient(
            LeaseA,
            cleanupToken: deadline.Token,
            renewalBarrier: finishRenewal.Task,
            renewalStarted: () => renewalEntered.TrySetResult());
        await client.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: deadline.Token);
        await using (BlobDistributedLock owned = new(
                         client,
                         LeaseA,
                         20,
                         60,
                         "cadence-control",
                         Stopwatch.StartNew(),
                         clock))
        {
            Task renewal = RenewUntilStoppedAsync(owned, clock, stop.Token);
            try
            {
                for (int second = 0; second < 39; second++)
                {
                    await clock.AdvanceAndDrainAsync(TimeSpan.FromSeconds(1), deadline.Token, renewal);
                }

                Task advancing = clock.AdvanceAndDrainAsync(TimeSpan.FromSeconds(1), deadline.Token, renewal);
                await renewalEntered.Task.WaitAsync(deadline.Token);
                Assert.False(advancing.IsCompleted);
                Assert.Equal(BaseTime.AddSeconds(40), clock.GetUtcNow());
                finishRenewal.TrySetResult();
                await advancing;
                for (int second = 40; second < 61; second++)
                {
                    await clock.AdvanceAndDrainAsync(TimeSpan.FromSeconds(1), deadline.Token, renewal);
                }

                Assert.Equal(LeaseA, backend.ActiveLeaseId);
                Assert.Equal(4, clock.TimerRegistrations);
                Assert.Single(backend.Trace, item => item.StartsWith("renew ", StringComparison.Ordinal));
            }
            finally
            {
                finishRenewal.TrySetResult();
                await stop.CancelAsync();
                try
                {
                    await renewal;
                }
                catch (OperationCanceledException)
                {
                    // The control owns and observes cancellation of its renewal loop.
                }
            }
        }

        Assert.Null(backend.ActiveLeaseId);
        Output.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    BlockedClockStepObserved = true,
                    clock.TimerRegistrations,
                    LeaseTrace = backend.Trace.ToArray(),
                    ClockTrace = clock.Trace.ToArray(),
                    CleanupReleaseObserved = true,
                }));
    }

    /// <summary>
    ///     Models expiration, renewal of a retained expired ID and stale ownership after reacquisition.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task LeaseModelPreservesExpiryAndOwnerSemanticsAsync()
    {
        FakeTimeProvider clock = new(BaseTime);
        FiniteLeaseBackend backend = new(clock);
        IBlobLeaseClient a = backend.CreateClient(LeaseA, cleanupToken: TestContext.Current.CancellationToken);
        IBlobLeaseClient b = backend.CreateClient(LeaseB, cleanupToken: TestContext.Current.CancellationToken);
        CancellationToken token = TestContext.Current.CancellationToken;
        await a.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: token);
        await Assert.ThrowsAsync<RequestFailedException>(() => b.AcquireAsync(
            TimeSpan.FromSeconds(60),
            cancellationToken: token));
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Null(backend.ActiveLeaseId);
        await a.RenewAsync(cancellationToken: token);
        Assert.Equal(LeaseA, backend.ActiveLeaseId);
        clock.Advance(TimeSpan.FromSeconds(61));
        await b.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: token);
        RequestFailedException renew =
            await Assert.ThrowsAsync<RequestFailedException>(() => a.RenewAsync(cancellationToken: token));
        RequestFailedException release =
            await Assert.ThrowsAsync<RequestFailedException>(() => a.ReleaseAsync(cancellationToken: token));
        Assert.Equal(409, renew.Status);
        Assert.Equal(409, release.Status);
        Assert.Equal(LeaseB, backend.ActiveLeaseId);
        await b.ReleaseAsync(cancellationToken: token);
        Assert.Null(backend.ActiveLeaseId);
    }

    /// <summary>
    ///     The real repository rejects a second pending document for the same brook.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task PendingCursorRejectsCompetingAppendAsync()
    {
        int creates = 0;
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((CursorDocument value, PartitionKey? _, ItemRequestOptions? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                if (++creates > 1)
                {
                    throw new CosmosException(
                        "Pending document already exists",
                        HttpStatusCode.Conflict,
                        0,
                        "pending-control",
                        0);
                }

                return Task.FromResult(CreateItemResponse(value));
            });
        CosmosRepository repository = new(
            container.Object,
            new ImmediateRetryPolicy(),
            new CursorDocumentToStorageMapper(),
            new EventDocumentToStorageMapper());
        BrookKey key = new("type", "pending-control");
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        await repository.CreatePendingCursorAsync(key, new(0), 2, cancellation);
        CosmosException conflict = await Assert.ThrowsAsync<CosmosException>(() =>
            repository.CreatePendingCursorAsync(key, new(0), 2, cancellation));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(2, creates);
        Output.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    PendingCreates = creates,
                    SecondStatus = conflict.StatusCode,
                }));
    }
}