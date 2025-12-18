using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionGrain{TProjection, TBrook}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionGrain")]
public sealed class UxProjectionGrainTests
{
    private static Mock<IGrainContext> CreateDefaultGrainContext(
        string primaryKey = ValidPrimaryKey
    )
    {
        Mock<IGrainContext> mock = new();
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", primaryKey));
        return mock;
    }

    private static TestableUxProjectionGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IUxProjectionGrainFactory>? uxProjectionGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        string reducersHash = ReducersHash,
        Mock<ILogger>? loggerMock = null,
        string primaryKey = ValidPrimaryKey
    )
    {
        grainContextMock ??= CreateDefaultGrainContext(primaryKey);
        uxProjectionGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        loggerMock ??= new();
        return new(
            grainContextMock.Object,
            uxProjectionGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            reducersHash,
            loggerMock.Object);
    }

    private const string ReducersHash = "test-hash";

    private const string ValidPrimaryKey = "TestProjection|TEST.MODULE.STREAM|entity-123";

    /// <summary>
    ///     A testable implementation of <see cref="UxProjectionGrain{TProjection, TBrook}" />
    ///     that allows testing without full Orleans infrastructure.
    /// </summary>
    private sealed class TestableUxProjectionGrain : UxProjectionGrain<TestProjection, TestBrookDefinition>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableUxProjectionGrain" /> class.
        /// </summary>
        /// <param name="grainContext">The Orleans grain context.</param>
        /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
        /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
        /// <param name="reducersHash">The hash of the reducers for snapshot key construction.</param>
        /// <param name="logger">Logger instance.</param>
        public TestableUxProjectionGrain(
            IGrainContext grainContext,
            IUxProjectionGrainFactory uxProjectionGrainFactory,
            ISnapshotGrainFactory snapshotGrainFactory,
            string reducersHash,
            ILogger logger
        )
            : base(grainContext, uxProjectionGrainFactory, snapshotGrainFactory, reducersHash, logger)
        {
        }
    }

    /// <summary>
    ///     Verifies that constructor throws when grainContext is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionGrain(
            null!,
            uxProjectionGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            ReducersHash,
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
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionGrain(
            grainContextMock.Object,
            uxProjectionGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            ReducersHash,
            null!));
    }

    /// <summary>
    ///     Verifies that constructor throws when reducersHash is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenReducersHashIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionGrain(
            grainContextMock.Object,
            uxProjectionGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            null!,
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
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionGrain(
            grainContextMock.Object,
            uxProjectionGrainFactoryMock.Object,
            null!,
            ReducersHash,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that constructor throws when uxProjectionGrainFactory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenUxProjectionGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ILogger> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableUxProjectionGrain(
            grainContextMock.Object,
            null!,
            snapshotGrainFactoryMock.Object,
            ReducersHash,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that GetAsync fetches from snapshot when cursor position advances.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Cache Miss")]
    public async Task GetAsyncFetchesFromSnapshotWhenPositionAdvances()
    {
        // Arrange
        TestProjection projection1 = new(42);
        TestProjection projection2 = new(100);
        int callCount = 0;
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(() => new(5 + callCount++));
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.SetupSequence(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection1)
            .ReturnsAsync(projection2);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionKey>()))
            .Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        TestableUxProjectionGrain grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetAsync twice with advancing cursor
        TestProjection? result1 = await grain.GetAsync();
        TestProjection? result2 = await grain.GetAsync();

        // Assert - both calls should fetch from snapshot since cursor advanced
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(42, result1.Value);
        Assert.Equal(100, result2.Value);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    ///     Verifies that GetAsync returns cached projection when cursor position unchanged.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Cache Hit")]
    public async Task GetAsyncReturnsCachedProjectionWhenPositionUnchanged()
    {
        // Arrange
        TestProjection expectedProjection = new(42);
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(5));
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionKey>()))
            .Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        TestableUxProjectionGrain grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetAsync twice
        TestProjection? result1 = await grain.GetAsync();
        TestProjection? result2 = await grain.GetAsync();

        // Assert - second call should return cached value, snapshot only fetched once
        Assert.Same(result1, result2);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetAsync returns null when cursor position is NotSet.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("No Events")]
    public async Task GetAsyncReturnsNullWhenCursorPositionIsNotSet()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(-1));
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionKey>()))
            .Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        TestableUxProjectionGrain grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAsync();

        // Assert
        Assert.Null(result);
        snapshotGrainFactoryMock.Verify(
            f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync uses correct snapshot key with reducers hash.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Snapshot Key")]
    public async Task GetAsyncUsesCorrectSnapshotKeyWithReducersHash()
    {
        // Arrange
        const string customReducersHash = "custom-hash-123";
        SnapshotKey? capturedKey = null;
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(10));
        Mock<ISnapshotCacheGrain<TestProjection>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestProjection(1));
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionKey>()))
            .Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<TestProjection>(It.IsAny<SnapshotKey>()))
            .Callback<SnapshotKey>(key => capturedKey = key)
            .Returns(snapshotCacheGrainMock.Object);
        TestableUxProjectionGrain grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock,
            reducersHash: customReducersHash);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        await grain.GetAsync();

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal(customReducersHash, capturedKey.Value.Stream.ReducersHash);
        Assert.Equal(10, capturedKey.Value.Version);
        Assert.Equal("entity-123", capturedKey.Value.Stream.ProjectionId);
    }

    /// <summary>
    ///     Verifies that GrainContext property returns the injected context.
    /// </summary>
    [Fact]
    [AllureFeature("Grain Properties")]
    public void GrainContextReturnsInjectedContext()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = CreateDefaultGrainContext();
        TestableUxProjectionGrain grain = CreateGrain(grainContextMock);

        // Act & Assert
        Assert.Same(grainContextMock.Object, grain.GrainContext);
    }

    /// <summary>
    ///     Verifies that OnActivateAsync completes successfully with valid primary key.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncCompletesSuccessfullyWithValidPrimaryKey()
    {
        // Arrange
        TestableUxProjectionGrain grain = CreateGrain();

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.OnActivateAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}