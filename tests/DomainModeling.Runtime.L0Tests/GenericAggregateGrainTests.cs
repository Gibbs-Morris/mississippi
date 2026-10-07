using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Writer;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="GenericAggregateGrain{TAggregate}" />.
/// </summary>
public class GenericAggregateGrainTests
{
    private const string TestEntityId = "entity-123";

    private const string TestReducerHash = "test-event-reducer-hash";

    private static async Task<GenericAggregateGrain<AggregateGrainTestAggregate>> CreateActivatedGrainAsync(
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        IOptions<AggregateEffectOptions>? effectOptions = null,
        IRootEventEffect<AggregateGrainTestAggregate>? rootEventEffect = null
    )
    {
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = CreateGrain(
            brookGrainFactoryMock: brookGrainFactoryMock,
            brookEventConverterMock: brookEventConverterMock,
            rootCommandHandlerMock: rootCommandHandlerMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock,
            effectOptions: effectOptions,
            rootEventEffect: rootEventEffect);
        await grain.OnActivateAsync(CancellationToken.None);
        return grain;
    }

    private static Mock<IGrainContext> CreateDefaultGrainContext()
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", TestEntityId));
        return mock;
    }

    private static GenericAggregateGrain<AggregateGrainTestAggregate> CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IGrainFactory>? grainFactoryMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IRootReducer<AggregateGrainTestAggregate>>? rootReducerMock = null,
        IOptions<AggregateEffectOptions>? effectOptions = null,
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>>? loggerMock = null,
        Mock<ISagaReminderRegistry>? sagaReminderRegistryMock = null,
        IEnumerable<IFireAndForgetEffectRegistration<AggregateGrainTestAggregate>>? fireAndForgetEffectRegistrations =
            null,
        IRootEventEffect<AggregateGrainTestAggregate>? rootEventEffect = null,
        bool throwOnNullContext = false
    )
    {
        if (throwOnNullContext)
        {
            grainContextMock = null!;
        }
        else
        {
            grainContextMock ??= CreateDefaultGrainContext();
        }

        grainFactoryMock ??= new();
        brookGrainFactoryMock ??= new();
        brookEventConverterMock ??= new();
        rootCommandHandlerMock ??= new();
        snapshotGrainFactoryMock ??= new();
        rootReducerMock ??= new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(TestReducerHash);
        effectOptions ??= Options.Create(new AggregateEffectOptions());
        loggerMock ??= new();
        sagaReminderRegistryMock ??= new();
        fireAndForgetEffectRegistrations ??= [];
        return new(
            throwOnNullContext ? null! : grainContextMock.Object,
            grainFactoryMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            rootReducerMock.Object,
            effectOptions,
            TimeProvider.System,
            loggerMock.Object,
            fireAndForgetEffectRegistrations,
            sagaReminderRegistryMock.Object,
            rootEventEffect);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators - needed for IAsyncEnumerable signature
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods - IAsyncEnumerable helper naming
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        params T[] items
    )
    {
        foreach (T item in items)
        {
            yield return item;
        }
    }
#pragma warning restore VSTHRD200
#pragma warning restore CS1998

    /// <summary>
    ///     Constructor should throw when brook grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookGrainFactoryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IRootReducer<AggregateGrainTestAggregate>> rootReducerMock = new();
        IOptions<AggregateEffectOptions> effectOptions = Options.Create(new AggregateEffectOptions());
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>> loggerMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        Assert.Throws<ArgumentNullException>(() => new GenericAggregateGrain<AggregateGrainTestAggregate>(
            grainContextMock.Object,
            grainFactoryMock.Object,
            null!,
            converterMock.Object,
            handlerMock.Object,
            snapshotFactoryMock.Object,
            rootReducerMock.Object,
            effectOptions,
            TimeProvider.System,
            loggerMock.Object,
            [],
            reminderRegistryMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when grain context is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => CreateGrain(throwOnNullContext: true));
    }

    /// <summary>
    ///     ExecuteAsync should dispatch command to root command handler.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDispatchesCommandToHandler()
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>()));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock);
        AggregateGrainTestCommand command = new("test");
        OperationResult result = await grain.ExecuteAsync(command, CancellationToken.None);
        Assert.True(result.Success);
        handlerMock.Verify(h => h.Handle(command, null), Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should dispatch effects when root event effect is configured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDispatchesEffectsWhenConfigured()
    {
        // Arrange
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheMock = new();
        Mock<IRootEventEffect<AggregateGrainTestAggregate>> effectMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(1, "updated"));
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        AggregateGrainTestEvent testEvent = new("test-data");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { testEvent }));
        ImmutableArray<BrookEvent> brookEvents = ImmutableArray.Create(
            new BrookEvent
            {
                Id = "event-1",
                EventType = "TestEvent",
            });
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(brookEvents);

        // Setup effect mock to return no yielded events (just verify it's called)
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<object>());
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock,
            rootEventEffect: effectMock.Object);
        AggregateGrainTestCommand command = new("test");

        // Act
        OperationResult result = await grain.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        effectMock.Verify(
            e => e.DispatchAsync(
                testEvent,
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should dispatch fire-and-forget effects when registrations exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncDispatchesFireAndForgetEffectsWhenRegistered()
    {
        // Arrange
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<IFireAndForgetEffectRegistration<AggregateGrainTestAggregate>> registrationMock = new();
        Mock<IGrainFactory> grainFactoryMock = new();
        AggregateGrainTestEvent testEvent = new("dispatched");
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(5));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(10, "test"));
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(ImmutableArray.Create(new BrookEvent()));
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { testEvent }));
        registrationMock.Setup(r => r.EventType).Returns(typeof(AggregateGrainTestEvent));
        registrationMock.Setup(r => r.EffectTypeName).Returns("TestEffect");
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IRootReducer<AggregateGrainTestAggregate>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(TestReducerHash);
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>> loggerMock = new();
        Mock<ISagaReminderRegistry> reminderRegistryMock = new();
        IFireAndForgetEffectRegistration<AggregateGrainTestAggregate>[] registrations = [registrationMock.Object];
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = new(
            grainContextMock.Object,
            grainFactoryMock.Object,
            brookFactoryMock.Object,
            converterMock.Object,
            handlerMock.Object,
            snapshotFactoryMock.Object,
            rootReducerMock.Object,
            Options.Create(new AggregateEffectOptions()),
            TimeProvider.System,
            loggerMock.Object,
            registrations,
            reminderRegistryMock.Object);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("test"),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        registrationMock.Verify(
            r => r.Dispatch(
                grainFactoryMock.Object,
                testEvent,
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>()),
            Times.Once);
    }

    /// <summary>
    ///     An append failure without a confirmed position must preserve the previous cached state.
    /// </summary>
    /// <param name="isPublicationFailure">Whether the failure is an unpositioned publication exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsyncKeepsPreviousPositionWhenAppendFailureIsUnconfirmed(
        bool isPublicationFailure
    )
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotMock = new();
        AggregateGrainTestAggregate initialState = new(1, "before");
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(5));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(initialState);
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotMock.Object);
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([new AggregateGrainTestEvent("attempted")]));
        ImmutableArray<BrookEvent> brookEvents =
        [
            new()
            {
                Id = "event",
                EventType = "TestEvent",
            },
        ];
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(brookEvents);
        Exception appendFailure = isPublicationFailure
            ? new BrookCursorPublicationException("No position supplied.", new InvalidOperationException("Failure."))
            : new InvalidOperationException("Unconfirmed storage failure.");
        writerMock.Setup(w => w.AppendEventsAsync(brookEvents, new BrookPosition(5), It.IsAny<CancellationToken>()))
            .ThrowsAsync(appendFailure);
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock);
        Exception observed = await Assert.ThrowsAnyAsync<Exception>(() => grain.ExecuteAsync(
            new AggregateGrainTestCommand("attempted"),
            CancellationToken.None));
        Assert.Same(appendFailure, observed);
        Assert.Same(initialState, await grain.GetStateAsync(CancellationToken.None));
        snapshotFactoryMock.Verify(
            f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.Is<SnapshotKey>(k => k.Version == 5)),
            Times.Exactly(2));
        cursorMock.Verify(c => c.GetLatestPositionAsync(), Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should persist events when handler returns events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncPersistsEventsWhenHandlerReturnsEvents()
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        AggregateGrainTestEvent testEvent = new("test-data");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { testEvent }));
        ImmutableArray<BrookEvent> brookEvents = ImmutableArray.Create(
            new BrookEvent
            {
                Id = "event-1",
                EventType = "TestEvent",
            });
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(brookEvents);
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            brookEventConverterMock: converterMock);
        AggregateGrainTestCommand command = new("test");
        OperationResult result = await grain.ExecuteAsync(command, CancellationToken.None);
        Assert.True(result.Success);
        writerMock.Verify(w => w.AppendEventsAsync(brookEvents, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should persist events yielded by effects.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncPersistsEventsYieldedByEffects()
    {
        // Arrange
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheMock = new();
        Mock<IRootEventEffect<AggregateGrainTestAggregate>> effectMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(0));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(1, "updated"));
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        AggregateGrainTestEvent testEvent = new("original");
        AggregateGrainTestEvent yieldedEvent = new("yielded-by-effect");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { testEvent }));
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns((BrookKey _, IReadOnlyList<object> events) => ImmutableArray.Create(
                new BrookEvent
                {
                    Id = "event-" + events[0].GetHashCode(),
                    EventType = "TestEvent",
                }));
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                testEvent,
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable<object>(yieldedEvent));
        effectMock.Setup(e => e.DispatchAsync(
                yieldedEvent,
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<object>());
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock,
            rootEventEffect: effectMock.Object);

        // Act
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("test"),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Original event write + yielded event write = 2 writes
        writerMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    ///     ExecuteAsync should respect custom MaxEffectIterations from options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncRespectsCustomMaxEffectIterations()
    {
        // Arrange - set a custom low limit of 2 iterations
        AggregateEffectOptions customOptions = new()
        {
            MaxEffectIterations = 2,
        };
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheMock = new();
        Mock<IRootEventEffect<AggregateGrainTestAggregate>> effectMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(0));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(1, "state"));
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);

        // Each effect yields another event, creating a chain that would go forever
        AggregateGrainTestEvent initialEvent = new("initial");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { initialEvent }));
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns((BrookKey _, IReadOnlyList<object> events) => ImmutableArray.Create(
                new BrookEvent
                {
                    Id = "event-" + Guid.NewGuid(),
                    EventType = "TestEvent",
                }));

        // Effect always yields another event (infinite chain without limit)
        int dispatchCount = 0;
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                It.IsAny<object>(),
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                dispatchCount++;
                return ToAsyncEnumerable<object>(new AggregateGrainTestEvent($"chained-{dispatchCount}"));
            });
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock,
            Options.Create(customOptions),
            effectMock.Object);

        // Act
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("test"),
            CancellationToken.None);

        // Assert - command should succeed but effect chain should stop at 2 iterations
        Assert.True(result.Success);

        // With limit of 2: iteration 1 dispatches initialEvent, iteration 2 dispatches chained-1
        // Then loop exits because iteration >= maxIterations
        // So we expect exactly 2 dispatch calls total
        Assert.Equal(2, dispatchCount);
    }

    /// <summary>
    ///     A committed publication failure must not leave reads or commands on the previous version.
    /// </summary>
    /// <param name="initialPosition">The position before the failed publication.</param>
    /// <param name="isLogEnabled">Whether error logging is enabled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(-1, true)]
    [InlineData(-1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public async Task ExecuteAsyncRetainsCommittedPositionAfterPublicationFailure(
        long initialPosition,
        bool isLogEnabled
    )
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> initialSnapshotMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> committedSnapshotMock = new();
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(isLogEnabled);
        BrookPosition committedPosition = new(initialPosition + 2);
        AggregateGrainTestAggregate initialState = new(0, "before");
        AggregateGrainTestAggregate committedState = new(2, "committed");
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(initialPosition));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        initialSnapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(initialState);
        committedSnapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(committedState);
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns((SnapshotKey key) => key.Version == committedPosition.Value
                ? committedSnapshotMock.Object
                : initialSnapshotMock.Object);
        AggregateGrainTestEvent testEvent = new("committed");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([testEvent, testEvent]));
        ImmutableArray<BrookEvent> brookEvents =
        [
            new()
            {
                Id = "event-1",
                EventType = "TestEvent",
            },
            new()
            {
                Id = "event-2",
                EventType = "TestEvent",
            },
        ];
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(brookEvents);
        BrookCursorPublicationException publicationFailure = new(
            committedPosition,
            new InvalidOperationException("Publication failed after commit."));
        writerMock.SetupSequence(w => w.AppendEventsAsync(
                brookEvents,
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(publicationFailure)
            .ReturnsAsync(new BrookPosition(committedPosition.Value + 2));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = CreateGrain(
            rootCommandHandlerMock: handlerMock,
            brookGrainFactoryMock: brookFactoryMock,
            snapshotGrainFactoryMock: snapshotFactoryMock,
            brookEventConverterMock: converterMock,
            loggerMock: loggerMock);
        await grain.OnActivateAsync(CancellationToken.None);
        BrookCursorPublicationException observed = await Assert.ThrowsAsync<BrookCursorPublicationException>(() =>
            grain.ExecuteAsync(new AggregateGrainTestCommand("first"), CancellationToken.None));
        Assert.Same(publicationFailure, observed);
        object?[][] errorLogs = loggerMock.Invocations.Where(i => i.Method.Name == "Log")
            .Select(i => i.Arguments.ToArray())
            .ToArray();
        if (isLogEnabled)
        {
            object?[] log = Assert.Single(errorLogs);
            Assert.Equal(LogLevel.Error, log[0]);
            EventId eventId = Assert.IsType<EventId>(log[1]);
            Assert.Equal(24, eventId.Id);
            Assert.Equal("CommittedAppendPublicationFailed", eventId.Name);
            Dictionary<string, object?> fields = Assert
                .IsType<IEnumerable<KeyValuePair<string, object?>>>(log[2], false)
                .ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("TEST.AGGREGATES.BROOK|" + TestEntityId, fields["AggregateKey"]);
            Assert.Equal(committedPosition.Value, fields["Position"]);
            Assert.Equal(
                "Events for aggregate {AggregateKey} committed at position {Position}, but cursor publication failed",
                fields["{OriginalFormat}"]);
            Assert.Same(publicationFailure, log[3]);
        }
        else
        {
            Assert.Empty(errorLogs);
        }

        Assert.Same(committedState, await grain.GetStateAsync(CancellationToken.None));
        AggregateGrainTestCommand nextCommand = new("next");
        OperationResult nextResult = await grain.ExecuteAsync(nextCommand, committedPosition, CancellationToken.None);
        Assert.True(nextResult.Success);
        handlerMock.Verify(h => h.Handle(nextCommand, committedState), Times.Once);
        writerMock.Verify(
            w => w.AppendEventsAsync(brookEvents, committedPosition, It.IsAny<CancellationToken>()),
            Times.Once);
        cursorMock.Verify(c => c.GetLatestPositionAsync(), Times.Once);
    }

    /// <summary>
    ///     A committed effect append must remain visible after the effect failure is isolated.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncRetainsCommittedPositionAfterYieldedEventPublicationFailure()
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> committedSnapshotMock = new();
        Mock<IRootEventEffect<AggregateGrainTestAggregate>> effectMock = new();
        AggregateGrainTestAggregate committedState = new(2, "effect committed");
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(5));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        snapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(1, "before effect"));
        committedSnapshotMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(committedState);
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns((SnapshotKey key) => key.Version == 7 ? committedSnapshotMock.Object : snapshotMock.Object);
        AggregateGrainTestEvent testEvent = new("command event");
        AggregateGrainTestEvent yieldedEvent = new("effect event");
        handlerMock.SetupSequence(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([testEvent]))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([]));
        ImmutableArray<BrookEvent> brookEvents =
        [
            new()
            {
                Id = "event",
                EventType = "TestEvent",
            },
        ];
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(brookEvents);
        writerMock.SetupSequence(w => w.AppendEventsAsync(
                brookEvents,
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(6))
            .ThrowsAsync(
                new BrookCursorPublicationException(
                    new BrookPosition(7),
                    new InvalidOperationException("Publication failed.")));
        effectMock.Setup(e => e.EffectCount).Returns(1);
        effectMock.Setup(e => e.DispatchAsync(
                testEvent,
                It.IsAny<AggregateGrainTestAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable<object>(yieldedEvent));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock,
            rootEventEffect: effectMock.Object);
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("first"),
            CancellationToken.None);
        Assert.True(result.Success);
        Assert.Same(committedState, await grain.GetStateAsync(CancellationToken.None));
        AggregateGrainTestCommand nextCommand = new("next");
        OperationResult nextResult = await grain.ExecuteAsync(
            nextCommand,
            new BrookPosition(7),
            CancellationToken.None);
        Assert.True(nextResult.Success);
        handlerMock.Verify(h => h.Handle(nextCommand, committedState), Times.Once);
        writerMock.Verify(
            w => w.AppendEventsAsync(brookEvents, new BrookPosition(6), It.IsAny<CancellationToken>()),
            Times.Once);
        cursorMock.Verify(c => c.GetLatestPositionAsync(), Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should return failure when handler returns failure.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncReturnsFailureWhenHandlerFails()
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Fail<IReadOnlyList<object>>(AggregateErrorCodes.InvalidCommand, "Test failure"));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock);
        AggregateGrainTestCommand command = new("test");
        OperationResult result = await grain.ExecuteAsync(command, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     ExecuteAsync should not call effects when no root event effect is configured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncSkipsEffectsWhenNotConfigured()
    {
        // Arrange
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        AggregateGrainTestEvent testEvent = new("test-data");
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(new object[] { testEvent }));
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "event-1",
                        EventType = "TestEvent",
                    }));

        // No rootEventEffect passed - should be null
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            brookEventConverterMock: converterMock);

        // Act
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("test"),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Only the original event write
        writerMock.Verify(
            w => w.AppendEventsAsync(It.IsAny<ImmutableArray<BrookEvent>>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should skip fire-and-forget dispatch when no registrations exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncSkipsFireAndForgetDispatchWhenNoRegistrations()
    {
        // Arrange
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        Mock<IBrookWriterGrain> writerMock = new();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(0));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        brookFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerMock.Object);
        converterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(ImmutableArray.Create(new BrookEvent()));
        snapshotCacheMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateGrainTestAggregate(0, "init"));
        snapshotFactoryMock.Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheMock.Object);
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>()));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock,
            snapshotFactoryMock,
            converterMock);

        // Act
        OperationResult result = await grain.ExecuteAsync(
            new AggregateGrainTestCommand("test"),
            CancellationToken.None);

        // Assert - should complete without errors and without calling any registration (no registrations exist)
        Assert.True(result.Success);
    }

    /// <summary>
    ///     ExecuteAsync should throw when command is null.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncThrowsWhenCommandIsNull()
    {
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.ExecuteAsync(null!, CancellationToken.None));
    }

    /// <summary>
    ///     ExecuteAsync with expected version should fail when version mismatch.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithVersionMismatchReturnsConcurrencyError()
    {
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(5));
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorMock.Object);
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            handlerMock,
            brookFactoryMock);
        AggregateGrainTestCommand command = new("test");
        OperationResult result = await grain.ExecuteAsync(command, new BrookPosition(3), CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.ConcurrencyConflict, result.ErrorCode);
        handlerMock.Verify(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()), Times.Never);
    }

    /// <summary>
    ///     OnActivateAsync should set up brook key from entity ID.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnActivateAsyncSetsBrookKeyFromEntityId()
    {
        Mock<IBrookGrainFactory> brookFactoryMock = new();
        Mock<IBrookCursorGrain> cursorMock = new();
        BrookKey? capturedKey = null;
        cursorMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()))
            .Callback<BrookKey>(k => capturedKey = k)
            .Returns(cursorMock.Object);
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        handlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>()));
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = await CreateActivatedGrainAsync(
            brookGrainFactoryMock: brookFactoryMock,
            rootCommandHandlerMock: handlerMock);
        AggregateGrainTestCommand command = new("test");
        await grain.ExecuteAsync(command, CancellationToken.None);
        Assert.NotNull(capturedKey);
        Assert.Equal("TEST.AGGREGATES.BROOK", capturedKey.Value.BrookName);
        Assert.Equal(TestEntityId, capturedKey.Value.EntityId);
    }
}