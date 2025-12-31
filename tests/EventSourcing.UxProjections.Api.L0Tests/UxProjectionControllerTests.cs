using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

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
        Mock<IUxProjectionGrainFactory>? factoryMock = null,
        string? ifNoneMatchHeader = null
    )
    {
        factoryMock ??= new();
        TestableController controller = new(
            factoryMock.Object,
            NullLogger<UxProjectionControllerBase<TestProjection, TestBrookDefinition>>.Instance);

        // Set up HttpContext for header access
        DefaultHttpContext httpContext = new();
        if (ifNoneMatchHeader is not null)
        {
            httpContext.Request.Headers.IfNoneMatch = ifNoneMatchHeader;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        return controller;
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
            ILogger<UxProjectionControllerBase<TestProjection, TestBrookDefinition>> logger
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
            NullLogger<UxProjectionControllerBase<TestProjection, TestBrookDefinition>>.Instance));
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
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(1));
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

    /// <summary>
    ///     Verifies that GetAsync returns ETag header with projection version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("ETag")]
    public async Task GetAsyncReturnsETagHeader()
    {
        // Arrange
        const long version = 42;
        TestProjection expectedProjection = new(100);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(version));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        ActionResult<TestProjection> result = await controller.GetAsync(TestEntityId);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(controller.Response.Headers.TryGetValue("ETag", out StringValues etag));
        Assert.Equal("\"42\"", etag.ToString());
    }

    /// <summary>
    ///     Verifies that GetAsync returns 304 Not Modified when If-None-Match matches.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("ETag")]
    public async Task GetAsyncReturns304WhenIfNoneMatchMatches()
    {
        // Arrange
        const long version = 42;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(version));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock, ifNoneMatchHeader: "\"42\"");

        // Act
        ActionResult<TestProjection> result = await controller.GetAsync(TestEntityId);

        // Assert
        StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync returns data when If-None-Match does not match.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("ETag")]
    public async Task GetAsyncReturnsDataWhenIfNoneMatchDoesNotMatch()
    {
        // Arrange
        const long currentVersion = 42;
        TestProjection expectedProjection = new(100);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(currentVersion));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock, ifNoneMatchHeader: "\"41\"");

        // Act
        ActionResult<TestProjection> result = await controller.GetAsync(TestEntityId);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestProjection projection = Assert.IsType<TestProjection>(okResult.Value);
        Assert.Equal(100, projection.Value);
        Assert.True(controller.Response.Headers.TryGetValue("ETag", out StringValues etag));
        Assert.Equal("\"42\"", etag.ToString());
    }

    /// <summary>
    ///     Verifies that GetAsync returns Cache-Control header.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("ETag")]
    public async Task GetAsyncReturnsCacheControlHeader()
    {
        // Arrange
        TestProjection expectedProjection = new(100);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(42));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(TestEntityId))
            .Returns(grainMock.Object);
        TestableController controller = CreateController(factoryMock);

        // Act
        await controller.GetAsync(TestEntityId);

        // Assert
        Assert.True(controller.Response.Headers.TryGetValue("Cache-Control", out StringValues cacheControl));
        Assert.Equal("private, must-revalidate", cacheControl.ToString());
    }
}