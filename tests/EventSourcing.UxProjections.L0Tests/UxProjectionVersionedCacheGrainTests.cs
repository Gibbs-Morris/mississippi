using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionVersionedCacheGrainBase{TProjection}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionVersionedCacheGrainBase")]
public sealed class UxProjectionVersionedCacheGrainTests
{
    private static Mock<IGrainContext> CreateDefaultGrainContext(
        string primaryKey = ValidPrimaryKey
    )
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", primaryKey));
        return mock;
    }

    private static TestableUxProjectionVersionedCacheGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        string? reducersHash = null,
        Mock<ILogger>? loggerMock = null,
        string primaryKey = ValidPrimaryKey
    )
    {
        grainContextMock ??= CreateDefaultGrainContext(primaryKey);
        snapshotGrainFactoryMock ??= new();
        reducersHash ??= "test-reducer-hash";
        loggerMock ??= new();
        return new(grainContextMock.Object, snapshotGrainFactoryMock.Object, reducersHash, loggerMock.Object);
    }

    private const string ValidPrimaryKey = "TestProjection|TEST.MODULE.STREAM|entity-123|42";

    /// <summary>
    ///     A testable implementation of <see cref="UxProjectionVersionedCacheGrainBase{TProjection}" />.
    /// </summary>
    [BrookName("TEST", "MODULE", "STREAM")]
    private sealed class TestableUxProjectionVersionedCacheGrain : UxProjectionVersionedCacheGrainBase<TestProjection>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableUxProjectionVersionedCacheGrain" /> class.
        /// </summary>
        /// <param name="grainContext">The Orleans grain context.</param>
        /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
        /// <param name="reducersHash">The hash of the reducers for snapshot key construction.</param>
        /// <param name="logger">Logger instance.</param>
        public TestableUxProjectionVersionedCacheGrain(
            IGrainContext grainContext,
            ISnapshotGrainFactory snapshotGrainFactory,
            string reducersHash,
            ILogger logger
        )
            : base(grainContext, snapshotGrainFactory, reducersHash, logger)
        {
        }

        /// <summary>
        ///     Gets the brook name from the attribute for testing.
        /// </summary>
        /// <returns>The brook name.</returns>
        public string GetBrookNameForTest() => BrookName;
    }

    /// <summary>
    ///     BrookName property should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void BrookNameReturnsValueFromAttribute()
    {
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain();
        string brookName = grain.GetBrookNameForTest();
        Assert.Equal("TEST.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     Constructor should initialize properties correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();

        // Act
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(grainContextMock);

        // Assert
        Assert.Same(grainContextMock.Object, grain.GrainContext);
    }

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionVersionedCacheGrain(
            null!,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionVersionedCacheGrain(
            grainContextMock.Object,
            snapshotGrainFactoryMock.Object,
            "test-hash",
            null!));
    }

    /// <summary>
    ///     Constructor should throw when reducersHash is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenReducersHashIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionVersionedCacheGrain(
            grainContextMock.Object,
            snapshotGrainFactoryMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when snapshotGrainFactory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenSnapshotGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionVersionedCacheGrain(
            grainContextMock.Object,
            null!,
            "test-hash",
            loggerMock.Object));
    }

    /// <summary>
    ///     GetAsync should return the projection loaded during activation.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("State Access")]
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
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(snapshotGrainFactoryMock: snapshotGrainFactoryMock);
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
    [AllureFeature("State Access")]
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
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(snapshotGrainFactoryMock: snapshotGrainFactoryMock);
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
    [AllureFeature("Caching")]
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
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetAsync twice
        TestProjection? result1 = await grain.GetAsync();
        TestProjection? result2 = await grain.GetAsync();

        // Assert - should return same cached result, only loaded once during activation
        Assert.Same(result1, result2);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     OnActivateAsync should parse versioned key correctly.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncParsesVersionedKeyCorrectly()
    {
        // Arrange
        TestProjection testProjection = new(0);
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(testProjection);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(snapshotGrainFactoryMock: snapshotGrainFactoryMock);

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
    [AllureFeature("Activation")]
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
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(snapshotGrainFactoryMock: snapshotGrainFactoryMock);

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
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncThrowsWhenPrimaryKeyIsInvalid()
    {
        // Arrange
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(primaryKey: "invalid-key-format");

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => grain.OnActivateAsync(CancellationToken.None));
    }

    /// <summary>
    ///     OnActivateAsync should use correct snapshot key format when loading.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Key Construction")]
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
        TestableUxProjectionVersionedCacheGrain grain = CreateGrain(
            snapshotGrainFactoryMock: snapshotGrainFactoryMock,
            reducersHash: "my-reducer-hash");

        // Act
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal("entity-123", capturedKey.Value.Stream.ProjectionId);
        Assert.Equal("my-reducer-hash", capturedKey.Value.Stream.ReducersHash);
        Assert.Equal(42, capturedKey.Value.Version);
    }
}