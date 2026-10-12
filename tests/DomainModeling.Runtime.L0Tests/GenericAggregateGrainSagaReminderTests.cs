using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Reader;
using Mississippi.Brooks.Abstractions.Writer;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for saga reminder behavior in <see cref="GenericAggregateGrain{TAggregate}" />.
/// </summary>
public sealed class GenericAggregateGrainSagaReminderTests
{
    private const string SagaReminderName = "mississippi.saga.resume";

    private const string TestEntityId = "saga-123";

    private const string TestReducerHash = "test-saga-reducer-hash";

    /// <summary>
    ///     Gets saga lifecycle case names that require reminder registration before append.
    /// </summary>
    public static TheoryData<string> ResumableSagaEventKinds =>
        new()
        {
            "Started",
            "StepCompleted",
            "StepFailed",
            "Compensating",
            "StepCompensated",
        };

    private static async Task<GenericAggregateGrain<TestSagaState>> CreateActivatedSagaGrainAsync(
        IReadOnlyList<object>? events = null,
        Mock<IBrookCursorGrain>? cursorMock = null,
        Mock<IBrookReaderGrain>? readerMock = null,
        Mock<IBrookWriterGrain>? writerMock = null,
        Mock<ISnapshotGrainFactory>? snapshotFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        Mock<ILogger<GenericAggregateGrain<TestSagaState>>>? loggerMock = null,
        Mock<ISagaReminderRegistry>? reminderRegistryMock = null,
        Mock<IRootEventEffect<TestSagaState>>? rootEventEffectMock = null,
        TimeProvider? timeProvider = null
    )
    {
        Mock<IRootCommandHandler<TestSagaState>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<TestSagaState?>()))
            .Returns(OperationResult.Ok(events ?? Array.Empty<object>()));
        Mock<IBrookGrainFactory> brookFactoryMock = CreateBrookFactoryMock(cursorMock, readerMock, writerMock);
        GenericAggregateGrain<TestSagaState> grain = CreateSagaGrain(
            brookGrainFactoryMock: brookFactoryMock,
            brookEventConverterMock: brookEventConverterMock,
            rootCommandHandlerMock: rootCommandHandlerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            loggerMock: loggerMock,
            reminderRegistryMock: reminderRegistryMock,
            rootEventEffectMock: rootEventEffectMock,
            timeProvider: timeProvider);
        await grain.OnActivateAsync(CancellationToken.None);
        return grain;
    }

    private static Mock<IBrookGrainFactory> CreateBrookFactoryMock(
        Mock<IBrookCursorGrain>? cursorMock = null,
        Mock<IBrookReaderGrain>? readerMock = null,
        Mock<IBrookWriterGrain>? writerMock = null
    )
    {
        cursorMock ??= new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        readerMock ??= new();
        writerMock ??= CreateWriterMock();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        return brookFactoryMock;
    }

    private static Mock<IBrookEventConverter> CreateConverterReturningStorageEvent()
    {
        Mock<IBrookEventConverter> converterMock = new();
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "event",
                        EventType = "Test",
                    }));
        return converterMock;
    }

    private static Mock<IGrainContext> CreateDefaultGrainContext()
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", TestEntityId));
        return mock;
    }

    private static Mock<IRootEventEffect<TestSagaState>> CreateEffectReturningNoEvents()
    {
        Mock<IRootEventEffect<TestSagaState>> effectMock = new();
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<TestSagaState>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<object>());
        return effectMock;
    }

    private static Mock<IBrookReaderGrain> CreateReaderReturningTailEvents(
        long position
    )
    {
        Mock<IBrookReaderGrain> readerMock = new();
        long readFrom = Math.Max(0, position - 1);
        readerMock.Setup(r => r.ReadEventsBatchAsync(
                It.Is<BrookPosition?>(from => from.HasValue && (from.Value.Value == readFrom)),
                It.Is<BrookPosition?>(readTo => readTo.HasValue && (readTo.Value.Value == position)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = $"event-{position}",
                        EventType = "Tail",
                    }));
        return readerMock;
    }

    private static Mock<IBrookReaderGrain> CreateReaderReturningTwoTailEvents()
    {
        Mock<IBrookReaderGrain> readerMock = new();
        readerMock.Setup(r => r.ReadEventsBatchAsync(
                It.Is<BrookPosition?>(readFrom => readFrom.HasValue && (readFrom.Value.Value == 0)),
                It.Is<BrookPosition?>(readTo => readTo.HasValue && (readTo.Value.Value == 1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ImmutableArray.Create(
                    new()
                    {
                        Id = "event-0",
                        EventType = "Started",
                    },
                    new BrookEvent
                    {
                        Id = "event-1",
                        EventType = "Input",
                    }));
        return readerMock;
    }

    private static Mock<ISagaReminderRegistry> CreateReminderRegistryMock()
    {
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        return reminderRegistryMock;
    }

    private static object CreateResumableSagaEvent(
        string eventKind
    ) =>
        eventKind switch
        {
            "Started" => CreateStartedEvent(),
            "StepCompleted" => new SagaStepCompleted
            {
                StepIndex = 0,
                StepName = "Debit",
                CompletedAt = new(2026, 5, 23, 12, 1, 0, TimeSpan.Zero),
            },
            "StepFailed" => new SagaStepFailed
            {
                StepIndex = 1,
                StepName = "Credit",
                ErrorCode = "ERR",
            },
            "Compensating" => new SagaCompensating
            {
                FromStepIndex = 0,
            },
            "StepCompensated" => new SagaStepCompensated
            {
                StepIndex = 0,
                StepName = "Debit",
            },
            var _ => throw new ArgumentOutOfRangeException(nameof(eventKind), eventKind, "Unknown saga event kind."),
        };

    private static TestSagaState CreateRunningState(
        Guid sagaId
    ) =>
        new()
        {
            SagaId = sagaId,
            Phase = SagaPhase.Running,
            LastCompletedStepIndex = -1,
            StepHash = "HASH",
        };

    private static GenericAggregateGrain<TestSagaState> CreateSagaGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IGrainFactory>? grainFactoryMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        Mock<IRootCommandHandler<TestSagaState>>? rootCommandHandlerMock = null,
        Mock<ISnapshotGrainFactory>? snapshotFactoryMock = null,
        Mock<IRootReducer<TestSagaState>>? rootReducerMock = null,
        Mock<ILogger<GenericAggregateGrain<TestSagaState>>>? loggerMock = null,
        Mock<ISagaReminderRegistry>? reminderRegistryMock = null,
        Mock<IRootEventEffect<TestSagaState>>? rootEventEffectMock = null,
        TimeProvider? timeProvider = null
    )
    {
        grainContextMock ??= CreateDefaultGrainContext();
        grainFactoryMock ??= new();
        brookGrainFactoryMock ??= CreateBrookFactoryMock();
        brookEventConverterMock ??= CreateConverterReturningStorageEvent();
        rootCommandHandlerMock ??= new();
        snapshotFactoryMock ??= new();
        rootReducerMock ??= new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(TestReducerHash);
        loggerMock ??= new();
        reminderRegistryMock ??= CreateReminderRegistryMock();
        return new(
            grainContextMock.Object,
            grainFactoryMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotFactoryMock.Object,
            rootReducerMock.Object,
            Options.Create(new AggregateEffectOptions()),
            timeProvider ?? TimeProvider.System,
            loggerMock.Object,
            [],
            reminderRegistryMock.Object,
            rootEventEffectMock?.Object);
    }

    private static SagaStartedEvent CreateStartedEvent() =>
        new()
        {
            SagaId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            StartedAt = new(2026, 5, 23, 12, 0, 0, TimeSpan.Zero),
            StepHash = "HASH",
        };

    private static Mock<IBrookWriterGrain> CreateWriterMock()
    {
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition());
        return writerMock;
    }

    private static async IAsyncEnumerable<object> InvokeReminderThenYieldNoEventsAsync(
        Func<Task> callback
    )
    {
        await callback();
        yield break;
    }

    private static async IAsyncEnumerable<object> YieldEventsAsync(
        params object[] events
    )
    {
        foreach (object eventData in events)
        {
            await Task.Yield();
            yield return eventData;
        }
    }

    private sealed record TestSagaCommand;

    private sealed record TestSagaInput(string TransferId);

    /// <summary>
    ///     Fails safe when reminder registration fails before appending a saga lifecycle event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDoesNotAppendSagaEventWhenReminderRegistrationFails()
    {
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .ThrowsAsync(new InvalidOperationException("reminder provider unavailable"));
        Mock<IBrookWriterGrain> writerMock = new();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [CreateStartedEvent()],
            writerMock: writerMock,
            reminderRegistryMock: reminderRegistryMock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.ExecuteAsync(
            new TestSagaCommand(),
            CancellationToken.None));
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Leaves the reminder registry untouched when appending non-lifecycle events fails.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDoesNotRollBackReminderWhenNonLifecycleAppendFails()
    {
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("append failed"));
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [new TestSagaInput("transfer-9")],
            writerMock: writerMock,
            reminderRegistryMock: reminderRegistryMock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.ExecuteAsync(
            new TestSagaCommand(),
            CancellationToken.None));
        reminderRegistryMock.Verify(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), It.IsAny<string>()), Times.Never);
        reminderRegistryMock.Verify(
            r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()),
            Times.Never);
        reminderRegistryMock.Verify(
            r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()),
            Times.Never);
    }

    /// <summary>
    ///     Does not update the reminder inside yielded-effect persistence.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDoesNotUpdateReminderBeforeAppendingYieldedLifecycleEvent()
    {
        SagaStartedEvent started = CreateStartedEvent();
        SagaStepCompleted completed = new()
        {
            StepIndex = 0,
            StepName = "Debit",
            CompletedAt = new(2026, 5, 23, 12, 1, 0, TimeSpan.Zero),
        };
        TestSagaState runningState = CreateRunningState(started.SagaId);
        List<string> calls = [];
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Callback(() => calls.Add("reminder"))
            .Returns(Task.CompletedTask);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("append"))
            .ReturnsAsync(new BrookPosition());
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(runningState);
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        Mock<IRootEventEffect<TestSagaState>> effectMock = new();
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<TestSagaState>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns<object, TestSagaState, string, long, CancellationToken>((eventData, _, _, _, _) =>
                ReferenceEquals(eventData, started) ? YieldEventsAsync(completed) : AsyncEnumerable.Empty<object>());
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [started],
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            reminderRegistryMock: reminderRegistryMock,
            rootEventEffectMock: effectMock);
        OperationResult result = await grain.ExecuteAsync(new TestSagaCommand(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal(["reminder", "append", "append"], calls);
        reminderRegistryMock.Verify(
            r => r.RegisterOrUpdateAsync(grain, SagaReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    /// <summary>
    ///     Keeps recovery protection when an append exception cannot prove whether events committed.
    /// </summary>
    /// <param name="failureKind">The kind of uncertain append failure.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("Error")]
    [InlineData("Timeout")]
    [InlineData("Cancellation")]
    public async Task ExecuteAsyncKeepsNewReminderWhenAppendOutcomeIsUnknown(
        string failureKind
    )
    {
        Exception appendFailure = failureKind switch
        {
            "Timeout" => new TimeoutException("append outcome is unknown"),
            "Cancellation" => new OperationCanceledException("append outcome is unknown"),
            var _ => new InvalidOperationException("append outcome is unknown"),
        };
        Mock<IGrainReminder> reminderMock = new();
        IGrainReminder? registeredReminder = null;
        Mock<ISagaReminderRegistry> registryMock = CreateReminderRegistryMock();
        registryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(() => registeredReminder);
        registryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Callback(() => registeredReminder = reminderMock.Object)
            .Returns(Task.CompletedTask);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(appendFailure);
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [CreateStartedEvent()],
            writerMock: writerMock,
            reminderRegistryMock: registryMock);
        Exception thrown = await Assert.ThrowsAnyAsync<Exception>(() => grain.ExecuteAsync(
            new TestSagaCommand(),
            TestContext.Current.CancellationToken));
        Assert.Same(appendFailure, thrown);
        Assert.Same(reminderMock.Object, registeredReminder);
        registryMock.Verify(
            r => r.RegisterOrUpdateAsync(grain, SagaReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
            Times.Once);
        registryMock.Verify(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()), Times.Never);
    }

    /// <summary>
    ///     Keeps the newly created wake-up after the writer confirms commit but publication fails.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncKeepsNewReminderWhenCommittedCursorPublicationFails()
    {
        BrookCursorPublicationException appendFailure = new(
            new BrookPosition(1),
            new TimeoutException("publication failed"));
        Mock<IGrainReminder> reminderMock = new();
        IGrainReminder? registeredReminder = null;
        Mock<ISagaReminderRegistry> registryMock = CreateReminderRegistryMock();
        registryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(() => registeredReminder);
        registryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Callback(() => registeredReminder = reminderMock.Object)
            .Returns(Task.CompletedTask);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(appendFailure);
        Mock<IRootEventEffect<TestSagaState>> effectMock = CreateEffectReturningNoEvents();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [CreateStartedEvent()],
            writerMock: writerMock,
            reminderRegistryMock: registryMock,
            rootEventEffectMock: effectMock);
        BrookCursorPublicationException thrown = await Assert.ThrowsAsync<BrookCursorPublicationException>(() =>
            grain.ExecuteAsync(new TestSagaCommand(), TestContext.Current.CancellationToken));
        Assert.Same(appendFailure, thrown);
        Assert.Same(reminderMock.Object, registeredReminder);
        registryMock.Verify(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()), Times.Never);
        effectMock.Verify(
            e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<TestSagaState>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Keeps a pre-existing saga reminder registered when the event append fails.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncKeepsPreExistingReminderWhenAppendFails()
    {
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = CreateReminderRegistryMock();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("append failed"));
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [CreateStartedEvent()],
            writerMock: writerMock,
            reminderRegistryMock: reminderRegistryMock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.ExecuteAsync(
            new TestSagaCommand(),
            CancellationToken.None));
        reminderRegistryMock.Verify(
            r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()),
            Times.Never);
    }

    /// <summary>
    ///     Registers the saga reminder when lifecycle events are mixed with other events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncRegistersReminderWhenBatchMixesLifecycleAndOtherEvents()
    {
        Mock<ISagaReminderRegistry> reminderRegistryMock = CreateReminderRegistryMock();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [new TestSagaInput("transfer-9"), CreateStartedEvent()],
            reminderRegistryMock: reminderRegistryMock);
        OperationResult result = await grain.ExecuteAsync(new TestSagaCommand(), CancellationToken.None);
        Assert.True(result.Success);
        reminderRegistryMock.Verify(
            r => r.RegisterOrUpdateAsync(grain, SagaReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    /// <summary>
    ///     Registers the deterministic saga reminder before appending resumable lifecycle events.
    /// </summary>
    /// <param name="eventKind">The saga lifecycle event kind.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [MemberData(nameof(ResumableSagaEventKinds))]
    public async Task ExecuteAsyncRegistersSagaReminderBeforeAppendingResumableLifecycleEvents(
        string eventKind
    )
    {
        object eventData = CreateResumableSagaEvent(eventKind);
        List<string> calls = [];
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Callback(() => calls.Add("reminder"))
            .Returns(Task.CompletedTask);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("append"))
            .ReturnsAsync(new BrookPosition());
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            [eventData],
            writerMock: writerMock,
            reminderRegistryMock: reminderRegistryMock);
        OperationResult result = await grain.ExecuteAsync(new TestSagaCommand(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal(["reminder", "append"], calls);
    }

    /// <summary>
    ///     Appends one compensating boundary for a failed step tail before dispatching compensation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAppendsOneCompensatingEventForFailedStepTail()
    {
        SagaStepFailed failed = new()
        {
            StepIndex = 2,
            StepName = "Ship",
            ErrorCode = "ERR",
        };
        List<IReadOnlyList<object>> convertedWrites = [];
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(4));
        Mock<IBrookReaderGrain> readerMock = CreateReaderReturningTailEvents(4);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(5));
        Mock<IBrookEventConverter> converterMock = new();
        converterMock.Setup(c => c.ToDomainEvent(It.IsAny<BrookEvent>())).Returns(failed);
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Callback<BrookKey, IReadOnlyList<object>>((_, events) => convertedWrites.Add(events))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "compensating",
                        EventType = "SagaCompensating",
                    }));
        TestSagaState runningState = CreateRunningState(Guid.NewGuid());
        TestSagaState compensatingState = runningState with
        {
            Phase = SagaPhase.Compensating,
        };
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken _) => compensatingState);
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        Mock<IRootEventEffect<TestSagaState>> effectMock = CreateEffectReturningNoEvents();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            reminderRegistryMock: reminderRegistryMock,
            rootEventEffectMock: effectMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        IReadOnlyList<object> writtenEvents = Assert.Single(convertedWrites);
        SagaCompensating compensating = Assert.IsType<SagaCompensating>(Assert.Single(writtenEvents));
        Assert.Equal(1, compensating.FromStepIndex);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.Is<BrookPosition?>(position => position.HasValue && (position.Value.Value == 4)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        effectMock.Verify(
            e => e.DispatchAsync(compensating, compensatingState, It.IsAny<string>(), 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Fails the saga when the latest confirmed tail event cannot be safely resumed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderFailsSagaWhenConfirmedTailCannotBeResumed()
    {
        DateTimeOffset now = new(2026, 5, 24, 10, 46, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        SagaMarkerEvent unsupportedTail = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(4));
        Mock<IBrookReaderGrain> readerMock = CreateReaderReturningTailEvents(4);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(5));
        List<IReadOnlyList<object>> convertedWrites = [];
        Mock<IBrookEventConverter> converterMock = new();
        converterMock.Setup(c => c.ToDomainEvent(It.IsAny<BrookEvent>())).Returns(unsupportedTail);
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Callback<BrookKey, IReadOnlyList<object>>((_, events) => convertedWrites.Add(events))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "failed",
                        EventType = "SagaFailed",
                    }));
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRunningState(Guid.NewGuid()));
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            reminderRegistryMock: reminderRegistryMock,
            timeProvider: timeProvider);
        await grain.ReceiveReminder(SagaReminderName, default);
        IReadOnlyList<object> writtenEvents = Assert.Single(convertedWrites);
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(writtenEvents));
        Assert.Equal("SAGA_RECOVERY_UNSAFE_TAIL", failed.ErrorCode);
        Assert.Contains(nameof(SagaMarkerEvent), failed.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(now, failed.FailedAt);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.Is<BrookPosition?>(position => position.HasValue && (position.Value.Value == 4)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        reminderRegistryMock.Verify(r => r.UnregisterAsync(grain, grainReminderMock.Object), Times.Once);
    }

    /// <summary>
    ///     Fails the saga when a confirmed cursor exists but the tail read returns no events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderFailsSagaWhenConfirmedTailReadIsEmpty()
    {
        DateTimeOffset now = new(2026, 5, 24, 10, 45, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(3));
        Mock<IBrookReaderGrain> readerMock = new();
        readerMock.Setup(r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableArray<BrookEvent>.Empty);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(4));
        List<IReadOnlyList<object>> convertedWrites = [];
        Mock<IBrookEventConverter> converterMock = new();
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Callback<BrookKey, IReadOnlyList<object>>((_, events) => convertedWrites.Add(events))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "failed",
                        EventType = "SagaFailed",
                    }));
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRunningState(Guid.NewGuid()));
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            reminderRegistryMock: reminderRegistryMock,
            timeProvider: timeProvider);
        await grain.ReceiveReminder(SagaReminderName, default);
        IReadOnlyList<object> writtenEvents = Assert.Single(convertedWrites);
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(writtenEvents));
        Assert.Equal("SAGA_RECOVERY_NO_TAIL_EVENTS", failed.ErrorCode);
        Assert.Contains("no tail events", failed.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(now, failed.FailedAt);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.Is<BrookPosition?>(position => position.HasValue && (position.Value.Value == 3)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        reminderRegistryMock.Verify(r => r.UnregisterAsync(grain, grainReminderMock.Object), Times.Once);
    }

    /// <summary>
    ///     Ignores unknown reminder names without touching storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderIgnoresUnknownReminderName()
    {
        Mock<IBrookCursorGrain> cursorMock = new();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(cursorMock: cursorMock);
        await grain.ReceiveReminder("other-reminder", default);
        cursorMock.Verify(c => c.GetLatestPositionConfirmedAsync(), Times.Never);
    }

    /// <summary>
    ///     Keeps recovery protection when the confirmed cursor or snapshot cannot be read.
    /// </summary>
    /// <param name="hasCursorFailure">Whether confirmation fails at the cursor rather than the snapshot.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiveReminderKeepsReminderWhenConfirmationFails(
        bool hasCursorFailure
    )
    {
        Mock<IGrainReminder> reminderMock = new();
        Mock<ISagaReminderRegistry> registryMock = CreateReminderRegistryMock();
        registryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(reminderMock.Object);
        Mock<IBrookCursorGrain> cursorMock = new();
        if (hasCursorFailure)
        {
            cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync())
                .ThrowsAsync(new TimeoutException("cursor unavailable"));
        }
        else
        {
            cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(8));
        }

        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotMock = new();
        snapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("snapshot unavailable"));
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotMock.Object);
        Mock<IBrookReaderGrain> readerMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            reminderRegistryMock: registryMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        registryMock.Verify(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()), Times.Never);
        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Skips reminder recovery when the same grain activation is already dispatching saga work.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderNoOpsWhenSagaWorkIsAlreadyActive()
    {
        SagaStartedEvent started = CreateStartedEvent();
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        TestSagaState runningState = CreateRunningState(started.SagaId);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(runningState);
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        GenericAggregateGrain<TestSagaState>? grain = null;
        Mock<IRootEventEffect<TestSagaState>> effectMock = new();
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                started,
                runningState,
                It.IsAny<string>(),
                0,
                It.IsAny<CancellationToken>()))
            .Returns(() => InvokeReminderThenYieldNoEventsAsync(() =>
                grain!.ReceiveReminder(SagaReminderName, default)));
        grain = await CreateActivatedSagaGrainAsync(
            [started],
            cursorMock,
            snapshotFactoryMock: snapshotFactoryMock,
            rootEventEffectMock: effectMock);
        OperationResult result = await grain.ExecuteAsync(new TestSagaCommand(), CancellationToken.None);
        Assert.True(result.Success);
        cursorMock.Verify(c => c.GetLatestPositionConfirmedAsync(), Times.Never);
    }

    /// <summary>
    ///     Retains protection while confirmation is old, then resumes a start confirmed after reactivation.
    /// </summary>
    /// <param name="hasCommittedStart">Whether a later confirmed read reveals the committed saga start.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiveReminderReconcilesUnknownAppendAfterReactivation(
        bool hasCommittedStart
    )
    {
        SagaStartedEvent started = CreateStartedEvent();
        Mock<IGrainReminder> reminderMock = new();
        IGrainReminder? registeredReminder = null;
        Mock<ISagaReminderRegistry> registryMock = CreateReminderRegistryMock();
        registryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(() => registeredReminder);
        registryMock.Setup(r => r.RegisterOrUpdateAsync(
                It.IsAny<IGrainBase>(),
                SagaReminderName,
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()))
            .Callback(() => registeredReminder = reminderMock.Object)
            .Returns(Task.CompletedTask);
        registryMock.Setup(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), reminderMock.Object))
            .Callback(() => registeredReminder = null)
            .Returns(Task.CompletedTask);
        Mock<IBrookWriterGrain> writerMock = new();
        writerMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("append acknowledgement was lost"));
        GenericAggregateGrain<TestSagaState> originalGrain = await CreateActivatedSagaGrainAsync(
            [started],
            writerMock: writerMock,
            reminderRegistryMock: registryMock);
        await Assert.ThrowsAsync<TimeoutException>(() => originalGrain.ExecuteAsync(
            new TestSagaCommand(),
            TestContext.Current.CancellationToken));
        Assert.Same(reminderMock.Object, registeredReminder);
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.SetupSequence(c => c.GetLatestPositionConfirmedAsync())
            .ReturnsAsync(new BrookPosition(0))
            .ReturnsAsync(new BrookPosition(1));
        TestSagaState runningState = CreateRunningState(started.SagaId);
        Mock<ISnapshotCacheGrain<TestSagaState>> oldSnapshotMock = new();
        oldSnapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TestSagaState
                {
                    Phase = SagaPhase.NotStarted,
                });
        Mock<ISnapshotCacheGrain<TestSagaState>> runningSnapshotMock = new();
        runningSnapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(runningState);
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock
            .Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.Is<SnapshotKey>(key => key.Version == 0)))
            .Returns(oldSnapshotMock.Object);
        snapshotFactoryMock
            .Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.Is<SnapshotKey>(key => key.Version == 1)))
            .Returns(runningSnapshotMock.Object);
        Mock<IBrookReaderGrain> readerMock = new();
        readerMock.Setup(r => r.ReadEventsBatchAsync(
                It.Is<BrookPosition?>(from => from.HasValue && (from.Value.Value == 0)),
                It.Is<BrookPosition?>(to => to.HasValue && (to.Value.Value == 1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ImmutableArray.Create(
                    new()
                    {
                        Id = "existing-input",
                        EventType = "Input",
                    },
                    new BrookEvent
                    {
                        Id = "committed-start",
                        EventType = "Started",
                    }));
        Mock<IBrookEventConverter> converterMock = CreateConverterReturningStorageEvent();
        converterMock.SetupSequence(c => c.ToDomainEvent(It.IsAny<BrookEvent>()))
            .Returns(new TestSagaInput("transfer-9"))
            .Returns(started);
        Mock<IRootEventEffect<TestSagaState>> effectMock = CreateEffectReturningNoEvents();
        GenericAggregateGrain<TestSagaState> reactivatedGrain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            reminderRegistryMock: registryMock,
            rootEventEffectMock: effectMock);
        Assert.NotSame(originalGrain, reactivatedGrain);
        await reactivatedGrain.ReceiveReminder(SagaReminderName, default);
        Assert.Same(reminderMock.Object, registeredReminder);
        registryMock.Verify(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()), Times.Never);
        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        effectMock.Verify(
            e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<TestSagaState>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        if (hasCommittedStart)
        {
            await reactivatedGrain.ReceiveReminder(SagaReminderName, default);
        }

        cursorMock.Verify(
            c => c.GetLatestPositionConfirmedAsync(),
            hasCommittedStart ? Times.Exactly(2) : Times.Once());
        registryMock.Verify(r => r.UnregisterAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()), Times.Never);
        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            hasCommittedStart ? Times.Once() : Times.Never());
        effectMock.Verify(
            e => e.DispatchAsync(started, runningState, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()),
            hasCommittedStart ? Times.Once() : Times.Never());
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Same(reminderMock.Object, registeredReminder);
    }

    /// <summary>
    ///     Retains protection without recovery independently of debug logging.
    /// </summary>
    /// <param name="isDebugEnabled">Whether the debug log level is enabled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiveReminderRetainsProtectionWhenSagaPhaseIsNotStarted(
        bool isDebugEnabled
    )
    {
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(8));
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ILogger<GenericAggregateGrain<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(isDebugEnabled);
        object? capturedState = null;
        Exception? capturedException = null;
        loggerMock.Setup(l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => id.Id == 26),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(
                new InvocationAction(invocation =>
                {
                    capturedState = invocation.Arguments[2];
                    capturedException = invocation.Arguments[3] as Exception;
                }));
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        Mock<IBrookReaderGrain> readerMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TestSagaState
                {
                    Phase = SagaPhase.NotStarted,
                });
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            loggerMock: loggerMock,
            reminderRegistryMock: reminderRegistryMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        await grain.ReceiveReminder(SagaReminderName, default);
        reminderRegistryMock.Verify(r => r.UnregisterAsync(grain, grainReminderMock.Object), Times.Never);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(id =>
                    (id.Id == 26) &&
                    (id.Name == nameof(AggregateGrainLoggerExtensions.SagaReminderDeferredNotStarted))),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            isDebugEnabled ? Times.Exactly(2) : Times.Never());
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.Is<EventId>(id => id.Id == 26),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        if (isDebugEnabled)
        {
            IReadOnlyList<KeyValuePair<string, object?>> fields =
                Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(capturedState, false);
            Assert.Collection(
                fields,
                field =>
                {
                    Assert.Equal("ReminderName", field.Key);
                    Assert.Equal(SagaReminderName, Assert.IsType<string>(field.Value));
                },
                field =>
                {
                    Assert.Equal("AggregateKey", field.Key);
                    Assert.Equal(
                        BrookKey.ForType<TestSagaState>(TestEntityId).ToString(),
                        Assert.IsType<string>(field.Value));
                },
                field =>
                {
                    Assert.Equal("{OriginalFormat}", field.Key);
                    Assert.Equal(
                        "Reminder {ReminderName} found saga phase NotStarted for aggregate {AggregateKey}; deferring recovery and retaining reminder protection",
                        Assert.IsType<string>(field.Value));
                });
            Assert.Null(capturedException);
        }
        else
        {
            Assert.Null(capturedState);
        }

        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Unregisters the deterministic reminder without appending when the latest snapshot is terminal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderUnregistersAndNoOpsWhenSnapshotPhaseIsTerminal()
    {
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(8));
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<IBrookReaderGrain> readerMock = CreateReaderReturningTailEvents(8);
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                CreateRunningState(Guid.NewGuid()) with
                {
                    Phase = SagaPhase.Completed,
                });
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        Mock<IRootEventEffect<TestSagaState>> effectMock = CreateEffectReturningNoEvents();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            reminderRegistryMock: reminderRegistryMock,
            rootEventEffectMock: effectMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        reminderRegistryMock.Verify(r => r.UnregisterAsync(grain, grainReminderMock.Object), Times.Once);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        effectMock.Verify(
            e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<TestSagaState>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Unregisters an orphan reminder when no event was confirmed after registration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderUnregistersWhenConfirmedCursorIsUnset()
    {
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition());
        Mock<IGrainReminder> grainReminderMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        reminderRegistryMock.Setup(r => r.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderName))
            .ReturnsAsync(grainReminderMock.Object);
        Mock<IBrookReaderGrain> readerMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            writerMock: writerMock,
            reminderRegistryMock: reminderRegistryMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        reminderRegistryMock.Verify(r => r.UnregisterAsync(grain, grainReminderMock.Object), Times.Once);
        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Redispatches the start lifecycle boundary when the tail is start plus input.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderUsesConfirmedCursorSnapshotAndTailToRedispatchStartBoundary()
    {
        SagaStartedEvent started = CreateStartedEvent();
        SagaInputProvided<TestSagaInput> inputProvided = new()
        {
            SagaId = started.SagaId,
            Input = new("transfer-7"),
        };
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionConfirmedAsync()).ReturnsAsync(new BrookPosition(1));
        Mock<IBrookReaderGrain> readerMock = CreateReaderReturningTwoTailEvents();
        Mock<IBrookEventConverter> converterMock = new();
        converterMock.SetupSequence(c => c.ToDomainEvent(It.IsAny<BrookEvent>()))
            .Returns(started)
            .Returns(inputProvided);
        SnapshotKey? loadedSnapshotKey = null;
        Mock<ISnapshotCacheGrain<TestSagaState>> snapshotCacheMock = new();
        TestSagaState latestState = CreateRunningState(started.SagaId);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(latestState);
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestSagaState>(It.IsAny<SnapshotKey>()))
            .Callback<SnapshotKey>(key => loadedSnapshotKey = key)
            .Returns(snapshotCacheMock.Object);
        Mock<IRootEventEffect<TestSagaState>> effectMock = CreateEffectReturningNoEvents();
        GenericAggregateGrain<TestSagaState> grain = await CreateActivatedSagaGrainAsync(
            cursorMock: cursorMock,
            readerMock: readerMock,
            snapshotFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            rootEventEffectMock: effectMock);
        await grain.ReceiveReminder(SagaReminderName, default);
        cursorMock.Verify(c => c.GetLatestPositionConfirmedAsync(), Times.Once);
        Assert.NotNull(loadedSnapshotKey);
        Assert.Equal(1, loadedSnapshotKey.Value.Version);
        readerMock.Verify(
            r => r.ReadEventsBatchAsync(
                It.Is<BrookPosition?>(position => position.HasValue && (position.Value.Value == 0)),
                It.Is<BrookPosition?>(position => position.HasValue && (position.Value.Value == 1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        effectMock.Verify(
            e => e.DispatchAsync(started, latestState, It.IsAny<string>(), 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}