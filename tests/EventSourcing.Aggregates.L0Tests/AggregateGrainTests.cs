using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;
using Mississippi.EventSourcing.Writer;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Tests for <see cref="AggregateGrain{TState, TBrook}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Aggregate Grain")]
public class AggregateGrainTests
{
    private static async Task<TestableAggregateGrain> CreateActivatedGrainAsync(
        IServiceProvider? serviceProvider = null
    )
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext(serviceProvider);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        TestableAggregateGrain grain = CreateGrain(grainContextMock, brookGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);
        return grain;
    }

    private static Mock<IGrainContext> CreateDefaultGrainContext(
        IServiceProvider? serviceProvider = null
    )
    {
        Mock<IGrainContext> mock = new();
        serviceProvider ??= new ServiceCollection().BuildServiceProvider();
        mock.Setup(c => c.ActivationServices).Returns(serviceProvider);
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", "TEST.AGGREGATES.AggregateGrainTestBrook|entity-1"));
        return mock;
    }

    private static TestableAggregateGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISerializationProvider>? serializationProviderMock = null,
        Mock<IRootReducer<AggregateGrainTestState>>? rootReducerMock = null,
        Mock<IEventTypeRegistry>? eventTypeRegistryMock = null,
        Mock<ILogger>? loggerMock = null
    )
    {
        grainContextMock ??= CreateDefaultGrainContext();
        brookGrainFactoryMock ??= new();
        serializationProviderMock ??= new();
        rootReducerMock ??= new();
        eventTypeRegistryMock ??= new();
        loggerMock ??= new();
        return new(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            eventTypeRegistryMock.Object,
            loggerMock.Object);
    }

    private static async IAsyncEnumerable<BrookEvent> GetEmptyEventsAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    ///     Testable aggregate grain that exposes protected methods.
    /// </summary>
    private sealed class TestableAggregateGrain : AggregateGrain<AggregateGrainTestState, AggregateGrainTestBrook>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableAggregateGrain" /> class.
        /// </summary>
        public TestableAggregateGrain(
            IGrainContext grainContext,
            IBrookGrainFactory brookGrainFactory,
            ISerializationProvider serializationProvider,
            IRootReducer<AggregateGrainTestState> rootReducer,
            IEventTypeRegistry eventTypeRegistry,
            ILogger logger
        )
            : base(grainContext, brookGrainFactory, serializationProvider, rootReducer, eventTypeRegistry, logger)
        {
        }

        /// <summary>
        ///     Gets the brook name from the brook definition for testing.
        /// </summary>
        /// <returns>The brook name.</returns>
        public static string GetBrookName() => BrookName;

        /// <summary>
        ///     Exposes the protected ExecuteAsync method for testing.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The operation result.</returns>
        public Task<OperationResult> ExecuteCommandAsync<TCommand>(
            TCommand command,
            BrookPosition? expectedVersion = null,
            CancellationToken cancellationToken = default
        ) =>
            ExecuteAsync(command, expectedVersion, cancellationToken);

        /// <summary>
        ///     Exposes the protected ResolveEventType method for testing.
        /// </summary>
        /// <param name="eventTypeName">The event type name.</param>
        /// <returns>The resolved type or null.</returns>
        public Type? TestResolveEventType(
            string eventTypeName
        ) =>
            ResolveEventType(eventTypeName);
    }

    /// <summary>
    ///     BrookName property should return the brook name from the brook definition.
    /// </summary>
    [Fact]
    public void BrookNameReturnsValueFromBrookDefinition()
    {
        string brookName = TestableAggregateGrain.GetBrookName();
        Assert.Equal("TEST.AGGREGATES.TESTBROOK", brookName);
    }

    /// <summary>
    ///     Constructor should initialize properties correctly.
    /// </summary>
    [Fact]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        TestableAggregateGrain grain = CreateGrain(grainContextMock);
        Assert.Same(grainContextMock.Object, grain.GrainContext);
    }

    /// <summary>
    ///     Constructor should throw when brook grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookGrainFactoryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<IEventTypeRegistry> registryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            null!,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when event type registry is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEventTypeRegistryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when grain context is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<IEventTypeRegistry> registryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            null!,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<IEventTypeRegistry> registryMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            null!));
    }

    /// <summary>
    ///     Constructor should throw when root reducer is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenRootReducerIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> registryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            null!,
            registryMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when serialization provider is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenSerializationProviderIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<IEventTypeRegistry> registryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            null!,
            rootReducerMock.Object,
            registryMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     ExecuteAsync should fail when expected version does not match.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncFailsOnConcurrencyConflict()
    {
        TestableAggregateGrain grain = await CreateActivatedGrainAsync();

        // Current position is -1 (NotSet from empty brook), expect version 5
        OperationResult result = await grain.ExecuteCommandAsync(
            new AggregateGrainTestCommand("test"),
            new BrookPosition(5));
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.ConcurrencyConflict, result.ErrorCode);
    }

    /// <summary>
    ///     ExecuteAsync should fail when no command handler is registered.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncFailsWhenNoCommandHandlerRegistered()
    {
        TestableAggregateGrain grain = await CreateActivatedGrainAsync();
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.CommandHandlerNotFound, result.ErrorCode);
    }

    /// <summary>
    ///     ExecuteAsync should pass expected cursor position when not first write.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncPassesExpectedCursorPositionOnSubsequentWrites()
    {
        Mock<ICommandHandler<AggregateGrainTestCommand, AggregateGrainTestState>> handlerMock = new();
        List<object> events = new()
        {
            new AggregateGrainTestEvent("value1"),
        };
        handlerMock.Setup(h => h.Handle(It.IsAny<AggregateGrainTestCommand>(), It.IsAny<AggregateGrainTestState?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(events));
        Mock<IBrookWriterGrain> writerGrainMock = new();
        writerGrainMock.SetupSequence(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0))
            .ReturnsAsync(new BrookPosition(1));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        brookGrainFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerGrainMock.Object);
        Mock<ISerializationProvider> serializationProviderMock = new();
        serializationProviderMock.Setup(s => s.Serialize(It.IsAny<AggregateGrainTestEvent>()))
            .Returns(new byte[] { 1, 2, 3 }.AsMemory());
        serializationProviderMock.Setup(s => s.Format).Returns("json");
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()))
            .Returns(new AggregateGrainTestState(1, "value1"));
        Mock<IEventTypeRegistry> registryMock = new();
        registryMock.Setup(r => r.ResolveName(typeof(AggregateGrainTestEvent))).Returns("TEST.AGGREGATES.TESTEVENTV1");
        ServiceCollection services = new();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton(registryMock.Object);
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext(services.BuildServiceProvider());
        TestableAggregateGrain grain = new(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            new Mock<ILogger>().Object);
        await grain.OnActivateAsync(CancellationToken.None);

        // First write - position is NotSet
        await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test1"));

        // Second write - position should be passed
        await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test2"));
        writerGrainMock.Verify(
            w => w.AppendEventsAsync(It.IsAny<ImmutableArray<BrookEvent>>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        writerGrainMock.Verify(
            w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                new BrookPosition(0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should persist events and update state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncPersistsEventsAndUpdatesState()
    {
        Mock<ICommandHandler<AggregateGrainTestCommand, AggregateGrainTestState>> handlerMock = new();
        List<object> events = new()
        {
            new AggregateGrainTestEvent("value1"),
        };
        handlerMock.Setup(h => h.Handle(It.IsAny<AggregateGrainTestCommand>(), It.IsAny<AggregateGrainTestState?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(events));
        Mock<IBrookWriterGrain> writerGrainMock = new();
        writerGrainMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        brookGrainFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerGrainMock.Object);
        Mock<ISerializationProvider> serializationProviderMock = new();
        serializationProviderMock.Setup(s => s.Serialize(It.IsAny<AggregateGrainTestEvent>()))
            .Returns(new byte[] { 1, 2, 3 }.AsMemory());
        serializationProviderMock.Setup(s => s.Format).Returns("json");
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()))
            .Returns(new AggregateGrainTestState(1, "value1"));
        Mock<IEventTypeRegistry> registryMock = new();
        registryMock.Setup(r => r.ResolveName(typeof(AggregateGrainTestEvent))).Returns("TEST.AGGREGATES.TESTEVENTV1");
        ServiceCollection services = new();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton(registryMock.Object);
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext(services.BuildServiceProvider());
        TestableAggregateGrain grain = new(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            new Mock<ILogger>().Object);
        await grain.OnActivateAsync(CancellationToken.None);
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.True(result.Success);
        writerGrainMock.Verify(
            w => w.AppendEventsAsync(
                It.Is<ImmutableArray<BrookEvent>>(e => e.Length == 1),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
        rootReducerMock.Verify(r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()), Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should return failure from command handler.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncReturnsFailureFromCommandHandler()
    {
        Mock<ICommandHandler<AggregateGrainTestCommand, AggregateGrainTestState>> handlerMock = new();
        handlerMock.Setup(h => h.Handle(It.IsAny<AggregateGrainTestCommand>(), It.IsAny<AggregateGrainTestState?>()))
            .Returns(OperationResult.Fail<IReadOnlyList<object>>("CUSTOM_ERROR", "Handler failed"));
        ServiceCollection services = new();
        services.AddSingleton(handlerMock.Object);
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(services.BuildServiceProvider());
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.False(result.Success);
        Assert.Equal("CUSTOM_ERROR", result.ErrorCode);
        Assert.Equal("Handler failed", result.ErrorMessage);
    }

    /// <summary>
    ///     ExecuteAsync should succeed when handler returns empty events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncSucceedsWithEmptyEvents()
    {
        Mock<ICommandHandler<AggregateGrainTestCommand, AggregateGrainTestState>> handlerMock = new();
        handlerMock.Setup(h => h.Handle(It.IsAny<AggregateGrainTestCommand>(), It.IsAny<AggregateGrainTestState?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>()));
        ServiceCollection services = new();
        services.AddSingleton(handlerMock.Object);
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(services.BuildServiceProvider());
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.True(result.Success);
    }

    /// <summary>
    ///     ExecuteAsync should throw when command is null.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncThrowsWhenCommandIsNull()
    {
        TestableAggregateGrain grain = await CreateActivatedGrainAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            grain.ExecuteCommandAsync<AggregateGrainTestCommand>(null!));
    }

    /// <summary>
    ///     OnActivateAsync should handle empty brook correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnActivateAsyncHandlesEmptyBrook()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        TestableAggregateGrain grain = CreateGrain(
            grainContextMock,
            brookGrainFactoryMock,
            rootReducerMock: rootReducerMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Reducer should not be called for empty brook
        rootReducerMock.Verify(r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()), Times.Never);
    }

    /// <summary>
    ///     OnActivateAsync should hydrate state from brook events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnActivateAsyncHydratesStateFromBrookEvents()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IRootReducer<AggregateGrainTestState>> rootReducerMock = new();
        Mock<ILogger> loggerMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();

        // Set up reader to return events
        AggregateGrainTestEvent event1 = new("first");

        static async IAsyncEnumerable<BrookEvent> GetEventsAsync()
        {
            yield return new()
            {
                Id = "e1",
                EventType = "TEST.AGGREGATES.TESTEVENTV1",
                Data = new byte[] { 1, 2, 3 }.ToImmutableArray(),
            };
            yield return new()
            {
                Id = "e2",
                EventType = "TEST.AGGREGATES.TESTEVENTV1",
                Data = new byte[] { 4, 5, 6 }.ToImmutableArray(),
            };
            await Task.CompletedTask;
        }

        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);

        // Set up serialization provider to return the event when reading.
        // Uses It.IsAny<Type>() because the test doesn't need to verify the specific type -
        // we just need to ensure the mock returns the expected event for deserialization.
        serializationProviderMock.Setup(s => s.Deserialize(It.IsAny<Type>(), It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns(event1);

        // Set up reducer
        AggregateGrainTestState afterEvent1 = new(1, "first");
        AggregateGrainTestState afterEvent2 = new(2, "second");
        rootReducerMock.SetupSequence(r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()))
            .Returns(afterEvent1)
            .Returns(afterEvent2);

        // Set up event type registry
        Mock<IEventTypeRegistry> registryMock = new();
        registryMock.Setup(r => r.ResolveType(It.IsAny<string>())).Returns(typeof(AggregateGrainTestEvent));
        ServiceCollection services = new();
        services.AddSingleton(registryMock.Object);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        grainContextMock.Setup(c => c.ActivationServices).Returns(serviceProvider);
        TestableAggregateGrain grain = new(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            serializationProviderMock.Object,
            rootReducerMock.Object,
            registryMock.Object,
            loggerMock.Object);
        await grain.OnActivateAsync(CancellationToken.None);

        // Verify reducer was called for each event
        rootReducerMock.Verify(
            r => r.Reduce(It.IsAny<AggregateGrainTestState>(), It.IsAny<object>()),
            Times.Exactly(2));
    }

    /// <summary>
    ///     DeserializeEvent should throw when event type cannot be resolved.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnActivateAsyncThrowsWhenEventTypeCannotBeResolved()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();

        static async IAsyncEnumerable<BrookEvent> GetEventsWithUnknownTypeAsync()
        {
            yield return new()
            {
                Id = "e1",
                EventType = "Unknown.Event.v1",
                Data = new byte[] { 1, 2, 3 }.ToImmutableArray(),
            };
            await Task.CompletedTask;
        }

        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEventsWithUnknownTypeAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);

        // No registry registered, so ResolveEventType returns null
        TestableAggregateGrain grain = CreateGrain(grainContextMock, brookGrainFactoryMock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.OnActivateAsync(CancellationToken.None));
    }

    /// <summary>
    ///     ResolveEventType should return null when registry cannot resolve the event type name.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveEventTypeReturnsNullWhenRegistryCannotResolve()
    {
        Mock<IEventTypeRegistry> registryMock = new();
        registryMock.Setup(r => r.ResolveType("SomeEvent")).Returns((Type?)null);
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        TestableAggregateGrain grain = CreateGrain(
            grainContextMock,
            brookGrainFactoryMock,
            eventTypeRegistryMock: registryMock);
        await grain.OnActivateAsync(CancellationToken.None);
        Type? result = grain.TestResolveEventType("SomeEvent");
        Assert.Null(result);
    }

    /// <summary>
    ///     ResolveEventType should return type from registry.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveEventTypeReturnsTypeFromRegistry()
    {
        Mock<IEventTypeRegistry> registryMock = new();
        registryMock.Setup(r => r.ResolveType("TEST.AGGREGATES.TESTEVENTV1")).Returns(typeof(AggregateGrainTestEvent));
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(null, null, It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookReaderGrain(It.IsAny<BrookKey>())).Returns(readerGrainMock.Object);
        TestableAggregateGrain grain = CreateGrain(
            grainContextMock,
            brookGrainFactoryMock,
            eventTypeRegistryMock: registryMock);
        await grain.OnActivateAsync(CancellationToken.None);
        Type? result = grain.TestResolveEventType("TEST.AGGREGATES.TESTEVENTV1");
        Assert.Equal(typeof(AggregateGrainTestEvent), result);
    }
}