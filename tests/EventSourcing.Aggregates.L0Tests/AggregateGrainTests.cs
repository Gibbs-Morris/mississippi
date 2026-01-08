using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Abstractions.Cursor;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Writer;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateGrainBase{TSnapshot}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Aggregate Grain")]
public class AggregateGrainTests
{
    private static async Task<TestableAggregateGrain> CreateActivatedGrainAsync(
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null
    )
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        TestableAggregateGrain grain = CreateGrain(
            grainContextMock,
            brookGrainFactoryMock,
            brookEventConverterMock,
            rootCommandHandlerMock,
            snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);
        return grain;
    }

    private static Mock<IGrainContext> CreateDefaultGrainContext()
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", "TEST.AGGREGATES.AggregateGrainTestBrook|entity-1"));
        return mock;
    }

    private static TestableAggregateGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        string? reducersHash = null,
        Mock<ILogger>? loggerMock = null
    )
    {
        grainContextMock ??= CreateDefaultGrainContext();
        brookGrainFactoryMock ??= new();
        brookEventConverterMock ??= new();
        rootCommandHandlerMock ??= new();
        snapshotGrainFactoryMock ??= new();
        reducersHash ??= "test-reducer-hash";
        loggerMock ??= new();
        return new(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            reducersHash,
            loggerMock.Object);
    }

    /// <summary>
    ///     Testable aggregate grain that exposes protected methods.
    /// </summary>
    [BrookName("TEST", "AGGREGATES", "TESTBROOK")]
    private sealed class TestableAggregateGrain : AggregateGrainBase<AggregateGrainTestAggregate>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableAggregateGrain" /> class.
        /// </summary>
        public TestableAggregateGrain(
            IGrainContext grainContext,
            IBrookGrainFactory brookGrainFactory,
            IBrookEventConverter brookEventConverter,
            IRootCommandHandler<AggregateGrainTestAggregate> rootCommandHandler,
            ISnapshotGrainFactory snapshotGrainFactory,
            string reducersHash,
            ILogger logger
        )
            : base(
                grainContext,
                brookGrainFactory,
                brookEventConverter,
                rootCommandHandler,
                snapshotGrainFactory,
                reducersHash,
                logger)
        {
        }

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
    }

    /// <summary>
    ///     BrookName property should return the brook name from the brook definition attribute.
    /// </summary>
    [Fact]
    public void BrookNameReturnsValueFromBrookDefinitionAttribute()
    {
        string brookName = BrookNameHelper.GetBrookNameFromGrain(typeof(TestableAggregateGrain));
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
    ///     Constructor should throw when brook event converter is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookEventConverterIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            null!,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when brook grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookGrainFactoryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            null!,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when grain context is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            null!,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            "test-hash",
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
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            null!));
    }

    /// <summary>
    ///     Constructor should throw when reducers hash is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenReducersHashIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when root command handler is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenRootCommandHandlerIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            null!,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when snapshot grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenSnapshotGrainFactoryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        Mock<ILogger> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateGrain(
            grainContextMock.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            null!,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     ExecuteAsync should fail when expected version does not match.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncFailsOnConcurrencyConflict()
    {
        // Set up cursor grain to return NotSet position (no events)
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(brookGrainFactoryMock: brookGrainFactoryMock);

        // Current position is NotSet (new BrookPosition()), expect version 5
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
        // Set up cursor grain to return NotSet position (no events)
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(
                OperationResult.Fail<IReadOnlyList<object>>(
                    AggregateErrorCodes.CommandHandlerNotFound,
                    "No command handler registered for command type."));
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(rootCommandHandlerMock, brookGrainFactoryMock);
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.CommandHandlerNotFound, result.ErrorCode);
    }

    /// <summary>
    ///     ExecuteAsync should get state from snapshot grain when events exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncGetsStateFromSnapshotGrain()
    {
        // Set up cursor grain to return position 5 (events exist)
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(5));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);

        // Set up snapshot cache grain to return state
        AggregateGrainTestAggregate expectedState = new(5, "test-value");
        Mock<ISnapshotCacheGrain<AggregateGrainTestAggregate>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedState);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock
            .Setup(f => f.GetSnapshotCacheGrain<AggregateGrainTestAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);

        // Set up command handler to capture the state passed to it
        AggregateGrainTestAggregate? capturedState = null;
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Callback<object, AggregateGrainTestAggregate?>((
                _,
                s
            ) => capturedState = s)
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([]));
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(
            rootCommandHandlerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));

        // Verify the state was fetched from snapshot grain and passed to command handler
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(capturedState);
        Assert.Equal(expectedState.LastValue, capturedState!.LastValue);
    }

    /// <summary>
    ///     ExecuteAsync should persist events when command handler returns events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncPersistsEvents()
    {
        // Set up cursor grain to return NotSet position (no events)
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());

        // Set up writer grain to capture the append call
        Mock<IBrookWriterGrain> writerGrainMock = new();
        writerGrainMock.Setup(w => w.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        brookGrainFactoryMock.Setup(f => f.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writerGrainMock.Object);

        // Set up event converter
        Mock<IBrookEventConverter> brookEventConverterMock = new();
        brookEventConverterMock.Setup(c => c.ToStorageEvents(It.IsAny<BrookKey>(), It.IsAny<IReadOnlyList<object>>()))
            .Returns(
                ImmutableArray.Create(
                    new BrookEvent
                    {
                        Id = "e1",
                        EventType = "TEST.AGGREGATES.TESTEVENT.V1",
                    }));

        // Set up command handler to return events
        List<object> events = [new AggregateGrainTestEvent("value1")];
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(events));
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(
            rootCommandHandlerMock,
            brookGrainFactoryMock,
            brookEventConverterMock: brookEventConverterMock);
        OperationResult result = await grain.ExecuteCommandAsync(new AggregateGrainTestCommand("test"));
        Assert.True(result.Success);
        writerGrainMock.Verify(
            w => w.AppendEventsAsync(
                It.Is<ImmutableArray<BrookEvent>>(e => e.Length == 1),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     ExecuteAsync should return failure from command handler.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncReturnsFailureFromCommandHandler()
    {
        // Set up cursor grain to return NotSet position
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Fail<IReadOnlyList<object>>("CUSTOM_ERROR", "Handler failed"));
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(rootCommandHandlerMock, brookGrainFactoryMock);
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
        // Set up cursor grain to return NotSet position
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> rootCommandHandlerMock = new();
        rootCommandHandlerMock.Setup(h => h.Handle(It.IsAny<object>(), It.IsAny<AggregateGrainTestAggregate?>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>()));
        TestableAggregateGrain grain = await CreateActivatedGrainAsync(rootCommandHandlerMock, brookGrainFactoryMock);
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
    ///     OnActivateAsync should initialize keys correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnActivateAsyncInitializesKeysCorrectly()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        TestableAggregateGrain grain = CreateGrain(grainContextMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Grain should be activated without error - the keys are initialized
        // This test verifies OnActivateAsync doesn't throw
        Assert.NotNull(grain);
    }
}