using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionGrain{TProjection}" />.
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

    private static UxProjectionGrain<TestProjection> CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<IUxProjectionGrainFactory>? uxProjectionGrainFactoryMock = null,
        Mock<ILogger<UxProjectionGrain<TestProjection>>>? loggerMock = null,
        string primaryKey = ValidPrimaryKey
    )
    {
        grainContextMock ??= CreateDefaultGrainContext(primaryKey);
        uxProjectionGrainFactoryMock ??= new();
        loggerMock ??= new();
        return new(grainContextMock.Object, uxProjectionGrainFactoryMock.Object, loggerMock.Object);
    }

    private const string ValidPrimaryKey = "entity-123";

    /// <summary>
    ///     Verifies that constructor throws when grainContext is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        Mock<ILogger<UxProjectionGrain<TestProjection>>> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionGrain<TestProjection>(
            null!,
            uxProjectionGrainFactoryMock.Object,
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

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionGrain<TestProjection>(
            grainContextMock.Object,
            uxProjectionGrainFactoryMock.Object,
            null!));
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
        Mock<ILogger<UxProjectionGrain<TestProjection>>> loggerMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionGrain<TestProjection>(
            grainContextMock.Object,
            null!,
            loggerMock.Object));
    }

    /// <summary>
    ///     Verifies that GetAsync delegates to versioned cache grain when cursor has position.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Delegation")]
    public async Task GetAsyncDelegatesToVersionedCacheGrain()
    {
        // Arrange
        TestProjection expectedProjection = new(42);
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(5));
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrainMock = new();
        versionedCacheGrainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
            .Returns(cursorGrainMock.Object);
        uxProjectionGrainFactoryMock
            .Setup(f => f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()))
            .Returns(versionedCacheGrainMock.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
        versionedCacheGrainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetAsync fetches new version when cursor position advances.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Version Changes")]
    public async Task GetAsyncFetchesNewVersionWhenPositionAdvances()
    {
        // Arrange
        TestProjection projection1 = new(42);
        TestProjection projection2 = new(100);
        int callCount = 0;
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(() => new(5 + callCount++));
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrainMock1 = new();
        versionedCacheGrainMock1.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(projection1);
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrainMock2 = new();
        versionedCacheGrainMock2.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(projection2);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
            .Returns(cursorGrainMock.Object);
        uxProjectionGrainFactoryMock
            .SetupSequence(f =>
                f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()))
            .Returns(versionedCacheGrainMock1.Object)
            .Returns(versionedCacheGrainMock2.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act - call GetAsync twice with advancing cursor
        TestProjection? result1 = await grain.GetAsync();
        TestProjection? result2 = await grain.GetAsync();

        // Assert - both calls should fetch from versioned cache since cursor advanced
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(42, result1.Value);
        Assert.Equal(100, result2.Value);
    }

    /// <summary>
    ///     Verifies that GetAsync passes correct UxProjectionKey to factory.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Key Construction")]
    public async Task GetAsyncPassesCorrectVersionedKeyToFactory()
    {
        // Arrange
        UxProjectionVersionedCacheKey? capturedKey = null;
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(10));
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrainMock = new();
        versionedCacheGrainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestProjection(1));
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
            .Returns(cursorGrainMock.Object);
        uxProjectionGrainFactoryMock
            .Setup(f => f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()))
            .Callback<UxProjectionVersionedCacheKey>(key => capturedKey = key)
            .Returns(versionedCacheGrainMock.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        await grain.GetAsync();

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal(10, capturedKey.Value.Version.Value);
        Assert.Equal("entity-123", capturedKey.Value.EntityId);
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
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
            .Returns(cursorGrainMock.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAsync();

        // Assert
        Assert.Null(result);
        uxProjectionGrainFactoryMock.Verify(
            f => f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync returns null when version is NotSet.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Versioned Access")]
    public async Task GetAtVersionAsyncReturnsNullWhenVersionIsNotSet()
    {
        // Arrange
        BrookPosition notSetVersion = new(-1);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAtVersionAsync(notSetVersion);

        // Assert
        Assert.Null(result);
        uxProjectionGrainFactoryMock.Verify(
            f => f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync uses versioned cache grain for specific version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Versioned Access")]
    public async Task GetAtVersionAsyncUsesVersionedCacheGrain()
    {
        // Arrange
        BrookPosition requestedVersion = new(10);
        TestProjection expectedProjection = new(42);
        UxProjectionVersionedCacheKey? capturedKey = null;
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrainMock = new();
        versionedCacheGrainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock
            .Setup(f => f.GetUxProjectionVersionedCacheGrain<TestProjection>(It.IsAny<UxProjectionVersionedCacheKey>()))
            .Callback<UxProjectionVersionedCacheKey>(key => capturedKey = key)
            .Returns(versionedCacheGrainMock.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        TestProjection? result = await grain.GetAtVersionAsync(requestedVersion);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
        Assert.NotNull(capturedKey);
        Assert.Equal(10, capturedKey.Value.Version.Value);
    }

    /// <summary>
    ///     Verifies that GetLatestVersionAsync returns position from cursor grain.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Latest Version")]
    public async Task GetLatestVersionAsyncReturnsPositionFromCursor()
    {
        // Arrange
        BrookPosition expectedPosition = new(42);
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(expectedPosition);
        Mock<IUxProjectionGrainFactory> uxProjectionGrainFactoryMock = new();
        uxProjectionGrainFactoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
            .Returns(cursorGrainMock.Object);
        UxProjectionGrain<TestProjection> grain = CreateGrain(
            uxProjectionGrainFactoryMock: uxProjectionGrainFactoryMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        BrookPosition result = await grain.GetLatestVersionAsync();

        // Assert
        Assert.Equal(42, result.Value);
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
        UxProjectionGrain<TestProjection> grain = CreateGrain(grainContextMock);

        // Act & Assert
        Assert.Same(grainContextMock.Object, grain.GrainContext);
    }

    /// <summary>
    ///     Verifies that OnActivateAsync completes successfully with any entity ID format.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncCompletesSuccessfullyWithAnyEntityIdFormat()
    {
        // Arrange - any string is now a valid entity ID
        UxProjectionGrain<TestProjection> grain = CreateGrain(primaryKey: "any-valid-entity-id");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.OnActivateAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
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
        UxProjectionGrain<TestProjection> grain = CreateGrain();

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.OnActivateAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}