using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Writer;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="GenericAggregateGrain{TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Generic Aggregate Grain")]
public class GenericAggregateGrainTests
{
    private static async Task<GenericAggregateGrain<AggregateGrainTestAggregate>> CreateActivatedGrainAsync(
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null
    )
    {
        GenericAggregateGrain<AggregateGrainTestAggregate> grain = CreateGrain(
            brookGrainFactoryMock: brookGrainFactoryMock,
            brookEventConverterMock: brookEventConverterMock,
            rootCommandHandlerMock: rootCommandHandlerMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
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
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<IBrookEventConverter>? brookEventConverterMock = null,
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>>? rootCommandHandlerMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IRootReducer<AggregateGrainTestAggregate>>? rootReducerMock = null,
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>>? loggerMock = null,
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

        brookGrainFactoryMock ??= new();
        brookEventConverterMock ??= new();
        rootCommandHandlerMock ??= new();
        snapshotGrainFactoryMock ??= new();
        rootReducerMock ??= new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(TestReducerHash);
        loggerMock ??= new();
        return new(
            throwOnNullContext ? null! : grainContextMock!.Object,
            brookGrainFactoryMock.Object,
            brookEventConverterMock.Object,
            rootCommandHandlerMock.Object,
            snapshotGrainFactoryMock.Object,
            rootReducerMock.Object,
            loggerMock.Object);
    }

    private const string TestEntityId = "entity-123";

    private const string TestReducerHash = "test-reducer-hash";

    /// <summary>
    ///     Constructor should throw when brook grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookGrainFactoryIsNull()
    {
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookEventConverter> converterMock = new();
        Mock<IRootCommandHandler<AggregateGrainTestAggregate>> handlerMock = new();
        Mock<ISnapshotGrainFactory> snapshotFactoryMock = new();
        Mock<IRootReducer<AggregateGrainTestAggregate>> rootReducerMock = new();
        Mock<ILogger<GenericAggregateGrain<AggregateGrainTestAggregate>>> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new GenericAggregateGrain<AggregateGrainTestAggregate>(
            grainContextMock.Object,
            null!,
            converterMock.Object,
            handlerMock.Object,
            snapshotFactoryMock.Object,
            rootReducerMock.Object,
            loggerMock.Object));
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