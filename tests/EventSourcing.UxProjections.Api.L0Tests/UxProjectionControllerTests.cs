using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.UxProjections.Api.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionControllerBase{TProjection, TBrook}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections API")]
[AllureSubSuite("UxProjectionControllerBase")]
public sealed class UxProjectionControllerTests
{
    private static TestableController CreateController(
        Mock<IUxProjectionGrainFactory>? factoryMock = null
    )
    {
        factoryMock ??= new();
        return new(factoryMock.Object, NullLogger<TestableController>.Instance);
    }

    private const string TestEntityId = "entity-123";

    /// <summary>
    ///     A testable implementation of <see cref="UxProjectionControllerBase{TProjection, TBrook}" />.
    /// </summary>
    private sealed class TestableController : UxProjectionControllerBase<TestProjection, TestBrookDefinition>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableController" /> class.
        /// </summary>
        /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        public TestableController(
            IUxProjectionGrainFactory uxProjectionGrainFactory,
            ILogger<TestableController> logger
        )
            : base(uxProjectionGrainFactory, logger)
        {
        }
    }

    /// <summary>
    ///     Verifies that constructor accepts valid factory.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorAcceptsValidFactory()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> factoryMock = new();

        // Act
        TestableController controller = CreateController(factoryMock);

        // Assert - Controller was created successfully, verify no grain calls yet
        factoryMock.Verify(
            f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(It.IsAny<string>()),
            Times.Never);
        Assert.NotNull(controller);
    }

    /// <summary>
    ///     Verifies that constructor throws when factory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenFactoryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableController(
            null!,
            NullLogger<TestableController>.Instance));
    }

    /// <summary>
    ///     Verifies that GetAsync returns NotFound when projection is null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetAsync")]
    public async Task GetAsyncReturnsNotFoundWhenProjectionIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<TestProjection> result = await controller.GetAsync(TestEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    ///     Verifies that GetAsync returns Ok with projection when found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetAsync")]
    public async Task GetAsyncReturnsOkWhenProjectionFound()
    {
        // Arrange
        TestProjection expectedProjection = new(42);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<TestProjection> result = await controller.GetAsync(TestEntityId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestProjection projection = Assert.IsType<TestProjection>(okResult.Value);
        Assert.Equal(42, projection.Value);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync returns NotFound when projection is null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetAtVersionAsync")]
    public async Task GetAtVersionAsyncReturnsNotFoundWhenProjectionIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<TestProjection> result = await controller.GetAtVersionAsync(TestEntityId, 10);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync returns Ok with projection when found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetAtVersionAsync")]
    public async Task GetAtVersionAsyncReturnsOkWhenProjectionFound()
    {
        // Arrange
        const long version = 10;
        TestProjection expectedProjection = new(42);
        BrookPosition? capturedVersion = null;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .Callback<BrookPosition, CancellationToken>((
                v,
                _
            ) => capturedVersion = v)
            .ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<TestProjection> result = await controller.GetAtVersionAsync(TestEntityId, version);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestProjection projection = Assert.IsType<TestProjection>(okResult.Value);
        Assert.Equal(42, projection.Value);
        Assert.NotNull(capturedVersion);
        Assert.Equal(10, capturedVersion.Value.Value);
    }

    /// <summary>
    ///     Verifies that GetLatestVersionAsync returns NotFound when position is NotSet.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetLatestVersionAsync")]
    public async Task GetLatestVersionAsyncReturnsNotFoundWhenPositionIsNotSet()
    {
        // Arrange
        BrookPosition notSetPosition = new(-1);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(notSetPosition);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<BrookPosition> result = await controller.GetLatestVersionAsync(TestEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    ///     Verifies that GetLatestVersionAsync returns Ok with version when found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("GetLatestVersionAsync")]
    public async Task GetLatestVersionAsyncReturnsOkWhenVersionFound()
    {
        // Arrange
        BrookPosition expectedPosition = new(42);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedPosition);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<BrookPosition> result = await controller.GetLatestVersionAsync(TestEntityId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        BrookPosition position = Assert.IsType<BrookPosition>(okResult.Value);
        Assert.Equal(42, position.Value);
    }
}