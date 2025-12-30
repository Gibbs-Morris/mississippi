using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Tests for <see cref="SnapshotCacheGrainBase{TSnapshot, TBrook}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots")]
[AllureSubSuite("Snapshot Cache Grain")]
public sealed class SnapshotCacheGrainTests
{
    private static Mock<IGrainContext> CreateDefaultGrainContext()
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", "TEST.SNAPSHOTS.TestBrook|entity-1|abc123|5"));
        return mock;
    }

    private static TestableSnapshotCacheGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<ISnapshotStorageReader>? snapshotStorageReaderMock = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<IRootReducer<SnapshotCacheGrainTestState>>? rootReducerMock = null,
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>>? snapshotStateConverterMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        IOptions<SnapshotRetentionOptions>? retentionOptions = null,
        Mock<ILogger>? loggerMock = null
    )
    {
        grainContextMock ??= CreateDefaultGrainContext();
        snapshotStorageReaderMock ??= new();
        brookGrainFactoryMock ??= new();
        rootReducerMock ??= new();
        snapshotStateConverterMock ??= new();
        snapshotGrainFactoryMock ??= new();
        retentionOptions ??= Options.Create(new SnapshotRetentionOptions());
        loggerMock ??= new();
        return new(
            grainContextMock.Object,
            snapshotStorageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            snapshotStateConverterMock.Object,
            snapshotGrainFactoryMock.Object,
            retentionOptions,
            loggerMock.Object);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<BrookEvent> GetEmptyEventsAsync()
#pragma warning restore CS1998
    {
        yield break;
    }

    /// <summary>
    ///     Verifies that BrookName returns value from brook definition.
    /// </summary>
    [Fact]
    [AllureFeature("Grain Properties")]
    public void BrookNameReturnsValueFromBrookDefinition()
    {
        // Act
        string brookName = TestableSnapshotCacheGrain.GetBrookName();

        // Assert
        Assert.Equal("TEST.SNAPSHOTS.TestBrook", brookName);
    }

    /// <summary>
    ///     Verifies that constructor throws when brookGrainFactory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenBrookGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            null!,
            rootReducerMock.Object,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when grainContext is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            null!,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            options,
            null!));
    }

    /// <summary>
    ///     Verifies that constructor throws when retentionOptions is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenRetentionOptionsIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when rootReducer is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenRootReducerIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            null!,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when snapshotGrainFactory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenSnapshotGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            converterMock.Object,
            null!,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when snapshotStateConverter is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenSnapshotStateConverterIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            storageReaderMock.Object,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            null!,
            snapshotGrainFactoryMock.Object,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when snapshotStorageReader is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenSnapshotStorageReaderIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        IOptions<SnapshotRetentionOptions> options = Options.Create(new SnapshotRetentionOptions());
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableSnapshotCacheGrain(
            grainContextMock.Object,
            null!,
            brookGrainFactoryMock.Object,
            rootReducerMock.Object,
            converterMock.Object,
            snapshotGrainFactoryMock.Object,
            options,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that GetStateAsync returns the cached state after activation.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("State Access")]
    public async Task GetStateAsyncReturnsCachedState()
    {
        // Arrange
        const string reducerHash = "test-hash";
        SnapshotCacheGrainTestState expectedState = new()
        {
            Value = 123,
        };
        SnapshotEnvelope envelope = new()
        {
            Data = [1, 2, 3, 4],
            DataContentType = "application/json",
            ReducerHash = reducerHash,
        };
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(reducerHash);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        converterMock.Setup(c => c.FromEnvelope(envelope)).Returns(expectedState);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            rootReducerMock: rootReducerMock,
            snapshotStateConverterMock: converterMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetStateAsync multiple times
        SnapshotCacheGrainTestState result1 = await grain.GetStateAsync();
        SnapshotCacheGrainTestState result2 = await grain.GetStateAsync();

        // Assert - should return same cached state, only one deserialize call
        Assert.Same(result1, result2);
        converterMock.Verify(c => c.FromEnvelope(envelope), Times.Once);
    }

    /// <summary>
    ///     Verifies that activation loads state from storage when a matching snapshot exists.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncLoadsFromStorageWhenSnapshotExists()
    {
        // Arrange
        const string reducerHash = "test-hash";
        SnapshotCacheGrainTestState expectedState = new()
        {
            Value = 42,
        };
        SnapshotEnvelope envelope = new()
        {
            Data = [1, 2, 3, 4],
            DataContentType = "application/json",
            ReducerHash = reducerHash,
        };
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(reducerHash);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        converterMock.Setup(c => c.FromEnvelope(envelope)).Returns(expectedState);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            rootReducerMock: rootReducerMock,
            snapshotStateConverterMock: converterMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);
        SnapshotCacheGrainTestState result = await grain.GetStateAsync();

        // Assert
        Assert.Equal(expectedState.Value, result.Value);
        converterMock.Verify(c => c.FromEnvelope(envelope), Times.Once);
    }

    /// <summary>
    ///     Verifies that activation rebuilds state from the stream when no snapshot exists in storage.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncRebuildsFromStreamWhenNoSnapshotExists()
    {
        // Arrange
        const string reducerHash = "test-hash";
        SnapshotCacheGrainTestState expectedState = new()
        {
            Value = 99,
        };
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotEnvelope?)null);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(reducerHash);
        rootReducerMock.Setup(r => r.Reduce(It.IsAny<SnapshotCacheGrainTestState>(), It.IsAny<object>()))
            .Returns(expectedState);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookAsyncReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookAsyncReaderGrain(It.IsAny<BrookKey>()))
            .Returns(readerGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotPersisterGrain> persisterGrainMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotPersisterGrain(It.IsAny<SnapshotKey>()))
            .Returns(persisterGrainMock.Object);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            brookGrainFactoryMock: brookGrainFactoryMock,
            rootReducerMock: rootReducerMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);
        SnapshotCacheGrainTestState result = await grain.GetStateAsync();

        // Assert - state should be the initial state created by the grain
        Assert.NotNull(result);
    }

    /// <summary>
    ///     Verifies that activation rebuilds state when the stored snapshot has an empty reducer hash.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncRebuildsWhenReducerHashIsEmpty()
    {
        // Arrange
        const string currentReducerHash = "current-hash";
        SnapshotEnvelope envelope = new()
        {
            Data = [1, 2, 3, 4],
            DataContentType = "application/json",
            ReducerHash = string.Empty,
        };
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(currentReducerHash);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookAsyncReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookAsyncReaderGrain(It.IsAny<BrookKey>()))
            .Returns(readerGrainMock.Object);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotPersisterGrain> persisterGrainMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotPersisterGrain(It.IsAny<SnapshotKey>()))
            .Returns(persisterGrainMock.Object);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            brookGrainFactoryMock: brookGrainFactoryMock,
            rootReducerMock: rootReducerMock,
            snapshotStateConverterMock: converterMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert - should NOT have loaded from storage due to empty hash
        converterMock.Verify(c => c.FromEnvelope(It.IsAny<SnapshotEnvelope>()), Times.Never);

        // Should have triggered stream read
        readerGrainMock.Verify(
            r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that activation rebuilds state when the reducer hash does not match the stored snapshot.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncRebuildsWhenReducerHashMismatch()
    {
        // Arrange
        const string currentReducerHash = "current-hash";
        const string storedReducerHash = "old-hash";
        SnapshotEnvelope envelope = new()
        {
            Data = [1, 2, 3, 4],
            DataContentType = "application/json",
            ReducerHash = storedReducerHash,
        };
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(currentReducerHash);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookAsyncReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookAsyncReaderGrain(It.IsAny<BrookKey>()))
            .Returns(readerGrainMock.Object);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotPersisterGrain> persisterGrainMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotPersisterGrain(It.IsAny<SnapshotKey>()))
            .Returns(persisterGrainMock.Object);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            brookGrainFactoryMock: brookGrainFactoryMock,
            rootReducerMock: rootReducerMock,
            snapshotStateConverterMock: converterMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert - should NOT have loaded from storage due to hash mismatch
        converterMock.Verify(c => c.FromEnvelope(It.IsAny<SnapshotEnvelope>()), Times.Never);

        // Should have triggered stream read
        readerGrainMock.Verify(
            r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that activation requests background persistence when state is rebuilt.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Persistence")]
    public async Task OnActivateAsyncRequestsPersistenceWhenRebuilt()
    {
        // Arrange
        const string reducerHash = "test-hash";
        Mock<ISnapshotStorageReader> storageReaderMock = new();
        storageReaderMock.Setup(r => r.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotEnvelope?)null);
        Mock<IRootReducer<SnapshotCacheGrainTestState>> rootReducerMock = new();
        rootReducerMock.Setup(r => r.GetReducerHash()).Returns(reducerHash);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookAsyncReaderGrain> readerGrainMock = new();
        readerGrainMock.Setup(r => r.ReadEventsAsync(
                It.IsAny<BrookPosition?>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetEmptyEventsAsync);
        brookGrainFactoryMock.Setup(f => f.GetBrookAsyncReaderGrain(It.IsAny<BrookKey>()))
            .Returns(readerGrainMock.Object);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converterMock = new();
        converterMock.Setup(c => c.ToEnvelope(It.IsAny<SnapshotCacheGrainTestState>(), reducerHash))
            .Returns(
                new SnapshotEnvelope
                {
                    Data = [1, 2, 3],
                    DataContentType = "application/json",
                    ReducerHash = reducerHash,
                });
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotPersisterGrain> persisterGrainMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotPersisterGrain(It.IsAny<SnapshotKey>()))
            .Returns(persisterGrainMock.Object);
        TestableSnapshotCacheGrain grain = CreateGrain(
            snapshotStorageReaderMock: storageReaderMock,
            brookGrainFactoryMock: brookGrainFactoryMock,
            rootReducerMock: rootReducerMock,
            snapshotStateConverterMock: converterMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert - should have requested persistence
        persisterGrainMock.Verify(
            p => p.PersistAsync(It.IsAny<SnapshotEnvelope>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}