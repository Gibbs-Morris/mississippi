using System;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionVersionedCacheGrain{TProjection}" />.
/// </summary>
public sealed class UxProjectionVersionedCacheGrainTests
{
    private const string ValidPrimaryKey = "TEST.MODULE.STREAM|entity-123|42";

    private static Mock<IGrainContext> CreateDefaultGrainContext(
        string primaryKey = ValidPrimaryKey
    )
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", primaryKey));
        return mock;
    }

    private static Mock<IRootReducer<TestProjection>> CreateDefaultRootReducer(
        string reducersHash = "test-event-reducer-hash"
    )
    {
        Mock<IRootReducer<TestProjection>> mock = new();
        mock.Setup(r => r.GetReducerHash()).Returns(reducersHash);
        return mock;
    }

    private static UxProjectionVersionedCacheGrain<TestProjection> CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IRootReducer<TestProjection>>? rootReducerMock = null,
        Mock<ILogger<UxProjectionVersionedCacheGrain<TestProjection>>>? loggerMock = null,
        string primaryKey = ValidPrimaryKey,
        string reducersHash = "test-event-reducer-hash"
    )
    {
        grainContextMock ??= CreateDefaultGrainContext(primaryKey);
        snapshotGrainFactoryMock ??= new();
        rootReducerMock ??= CreateDefaultRootReducer(reducersHash);
        loggerMock ??= new();
        return new(grainContextMock.Object, snapshotGrainFactoryMock.Object, rootReducerMock.Object, loggerMock.Object);
    }

    /// <summary>
    ///     Constructor should initialize properties correctly.
    /// </summary>
    [Fact]
        public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();

        // Act
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(grainContextMock);

        // Assert
        Assert.Same(grainContextMock.Object, grain.GrainContext);
    }

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<IRootReducer<TestProjection>> rootReducerMock = CreateDefaultRootReducer();
        Mock<ILogger<UxProjectionVersionedCacheGrain<TestProjection>>> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheGrain<TestProjection>(
            null!,
            snapshotGrainFactoryMock.Object,
            rootReducerMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<IRootReducer<TestProjection>> rootReducerMock = CreateDefaultRootReducer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheGrain<TestProjection>(
            grainContextMock.Object,
            snapshotGrainFactoryMock.Object,
            rootReducerMock.Object,
            null!));
    }

    /// <summary>
    ///     Constructor should throw when rootReducer is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenRootReducerIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger<UxProjectionVersionedCacheGrain<TestProjection>>> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheGrain<TestProjection>(
            grainContextMock.Object,
            snapshotGrainFactoryMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when snapshotGrainFactory is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenSnapshotGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IRootReducer<TestProjection>> rootReducerMock = CreateDefaultRootReducer();
        Mock<ILogger<UxProjectionVersionedCacheGrain<TestProjection>>> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheGrain<TestProjection>(
            grainContextMock.Object,
            null!,
            rootReducerMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     GetAsync should return the projection loaded during activation.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task GetAsyncReturnsProjectionLoadedOnActivation()
    {
        // Arrange
        TestProjection expectedProjection = new(123);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjection);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Value);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     GetAsync should return the projection value loaded during activation.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task GetAsyncReturnsProjectionValueFromActivation()
    {
        // Arrange
        TestProjection expectedProjection = new(42);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjection);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
    }

    /// <summary>
    ///     GetAsync should return same cached projection on subsequent calls without reloading.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task GetAsyncReturnsSameProjectionOnSubsequentCalls()
    {
        // Arrange
        TestProjection expectedProjection = new(99);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjection);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetAsync twice
        TestProjection? result1 = await grain.GetAsync();
        TestProjection? result2 = await grain.GetAsync();

        // Assert - should return same cached result, only loaded once during activation
        Assert.Same(result1, result2);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     OnActivateAsync should extract brook name from key.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task OnActivateAsyncExtractsBrookNameFromKey()
    {
        // Arrange
        SnapshotKey? capturedKey = null;
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestProjection(1));
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Callback<SnapshotKey>(key => capturedKey = key)
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert - brook name should be extracted from the key (TEST.MODULE.STREAM from brookKey.Type)
        Assert.NotNull(capturedKey);
        Assert.Equal("TEST.MODULE.STREAM", capturedKey.Value.Stream.BrookName);
    }

    /// <summary>
    ///     OnActivateAsync should parse versioned key correctly.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task OnActivateAsyncParsesVersionedKeyCorrectly()
    {
        // Arrange
        TestProjection testProjection = new(0);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(testProjection);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.OnActivateAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     OnActivateAsync should pass cancellation token to snapshot cache grain.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task OnActivateAsyncPassesCancellationTokenToSnapshotGrain()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;
        TestProjection testProjection = new(0);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .Returns((
                CancellationToken ct
            ) =>
            {
                capturedToken = ct;
                return new(testProjection);
            });
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        // Act
        await grain.OnActivateAsync(expectedToken);

        // Assert
        Assert.Equal(expectedToken, capturedToken);
    }

    /// <summary>
    ///     OnActivateAsync should throw when primary key is invalid.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task OnActivateAsyncThrowsWhenPrimaryKeyIsInvalid()
    {
        // Arrange
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(primaryKey: "invalid-key-format");

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => grain.OnActivateAsync(CancellationToken.None));
    }

    /// <summary>
    ///     OnActivateAsync should use correct snapshot key format when loading.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
        public async Task OnActivateAsyncUsesCorrectSnapshotKeyFormat()
    {
        // Arrange
        SnapshotKey? capturedKey = null;
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestProjection(1));
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Callback<SnapshotKey>(key => capturedKey = key)
            .Returns(snapshotCacheGrainMock.Object);
        UxProjectionVersionedCacheGrain<TestProjection> grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock,
            reducersHash: "my-event-reducer-hash");

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal("entity-123", capturedKey.Value.Stream.EntityId);
        Assert.Equal("my-event-reducer-hash", capturedKey.Value.Stream.ReducersHash);
        Assert.Equal(42, capturedKey.Value.Version);
    }
}