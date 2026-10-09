using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

using Mississippi.Brooks.Abstractions;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;

using Moq;


namespace Mississippi.DomainModeling.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionControllerBase{TProjection, TDto}" />.
/// </summary>
public sealed class UxProjectionControllerWithMapperTests
{
    private const string TestEntityId = "entity-123";

    private static TestableControllerWithMapper CreateController(
        Mock<IUxProjectionGrainFactory>? factoryMock = null,
        Mock<IMapper<TestProjection, TestDto>>? mapperMock = null,
        string? ifNoneMatchHeader = null
    )
    {
        factoryMock ??= new();
        mapperMock ??= new();
        TestableControllerWithMapper controller = new(
            factoryMock.Object,
            mapperMock.Object,
            NullLogger<UxProjectionControllerBase<TestProjection, TestDto>>.Instance);

        // Set up HttpContext for header access
        DefaultHttpContext httpContext = new();
        if (ifNoneMatchHeader is not null)
        {
            httpContext.Request.Headers.IfNoneMatch = ifNoneMatchHeader;
        }

        controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    /// <summary>
    ///     A testable implementation of <see cref="UxProjectionControllerBase{TProjection, TDto}" />.
    /// </summary>
    private sealed class TestableControllerWithMapper : UxProjectionControllerBase<TestProjection, TestDto>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableControllerWithMapper" /> class.
        /// </summary>
        /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
        /// <param name="mapper">Mapper for converting projections to DTOs.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        public TestableControllerWithMapper(
            IUxProjectionGrainFactory uxProjectionGrainFactory,
            IMapper<TestProjection, TestDto> mapper,
            ILogger<UxProjectionControllerBase<TestProjection, TestDto>> logger
        )
            : base(uxProjectionGrainFactory, mapper, logger)
        {
        }
    }

    /// <summary>
    ///     Verifies that constructor accepts valid parameters.
    /// </summary>
    [Fact]
    public void ConstructorAcceptsValidParameters()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();

        // Act
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Assert - Controller was created successfully, verify no grain calls yet
        factoryMock.Verify(f => f.GetUxProjectionGrain<TestProjection>(It.IsAny<string>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
        Assert.NotNull(controller);
    }

    /// <summary>
    ///     Verifies that constructor throws when factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenFactoryIsNull()
    {
        // Arrange
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableControllerWithMapper(
            null!,
            mapperMock.Object,
            NullLogger<UxProjectionControllerBase<TestProjection, TestDto>>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableControllerWithMapper(
            factoryMock.Object,
            mapperMock.Object,
            null!));
    }

    /// <summary>
    ///     Verifies that constructor throws when mapper is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenMapperIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrainFactory> factoryMock = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableControllerWithMapper(
            factoryMock.Object,
            null!,
            NullLogger<UxProjectionControllerBase<TestProjection, TestDto>>.Instance));
    }

    /// <summary>
    ///     Verifies newly supported matching forms read current state once and skip DTO mapping.
    /// </summary>
    /// <param name="headerValues">The values of the If-None-Match header fields.</param>
    /// <returns>Asynchronous test task.</returns>
    [Theory]
    [InlineData("W/\"42\"")]
    [InlineData("\"41\", \"42\"")]
    [InlineData("\"42\", \"43\"")]
    [InlineData("\"41\", W/\"42\", \"43\"")]
    [InlineData("\"41\"", "\"42\"")]
    [InlineData("W/\"42\"", "\"43\"")]
    [InlineData(" \t\"41\" ,\t W/\"42\" \t")]
    [InlineData("*")]
    public async Task GetAsyncReturns304ForValidMatchingIfNoneMatchForms(
        params string[] headerValues
    )
    {
        // Arrange
        TestProjection projection = new(100);
        TestDto dto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(42));
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(projection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(projection)).Returns(dto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);
        controller.Request.Headers.IfNoneMatch = new(headerValues);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Null(result.Value);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once());
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync returns 304 Not Modified when If-None-Match matches.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturns304WhenIfNoneMatchMatches()
    {
        // Arrange
        const long version = 42;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(version));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock, "\"42\"");

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that nonmatching entity tags return the current projection and its cache headers.
    /// </summary>
    /// <param name="headerValues">The values of the If-None-Match header fields.</param>
    /// <returns>Asynchronous test task.</returns>
    [Theory]
    [InlineData("W/\"41\"")]
    [InlineData("\"41\", \"43\"")]
    [InlineData("\"41\"", "W/\"43\"")]
    [InlineData("\"41,42\"")]
    [InlineData("\"042\"")]
    public async Task GetAsyncReturnsBodyForNonmatchingIfNoneMatchForms(
        params string[] headerValues
    )
    {
        // Arrange
        TestProjection projection = new(100);
        TestDto dto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(42));
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(projection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(projection)).Returns(dto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);
        controller.Request.Headers.IfNoneMatch = new(headerValues);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, okResult.Value);
        Assert.Equal("\"42\"", controller.Response.Headers.ETag.ToString());
        Assert.Equal("private, must-revalidate", controller.Response.Headers.CacheControl.ToString());
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        mapperMock.Verify(m => m.Map(projection), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetAsync returns Cache-Control header.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsCacheControlHeader()
    {
        // Arrange
        TestProjection expectedProjection = new(100);
        TestDto expectedDto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(42));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(expectedProjection)).Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(controller.Response.Headers.TryGetValue("Cache-Control", out StringValues cacheControl));
        Assert.Equal("private, must-revalidate", cacheControl.ToString());
    }

    /// <summary>
    ///     Verifies that GetAsync returns data when If-None-Match does not match.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsDataWhenIfNoneMatchDoesNotMatch()
    {
        // Arrange
        const long currentVersion = 42;
        TestProjection expectedProjection = new(100);
        TestDto expectedDto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(currentVersion));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(expectedProjection)).Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock, "\"41\"");

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestDto dto = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal("Mapped: 100", dto.Name);
        Assert.True(controller.Response.Headers.TryGetValue("ETag", out StringValues etag));
        Assert.Equal("\"42\"", etag.ToString());
        mapperMock.Verify(m => m.Map(expectedProjection), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetAsync returns ETag header with projection version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsETagHeader()
    {
        // Arrange
        const long version = 42;
        TestProjection expectedProjection = new(100);
        TestDto expectedDto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(version));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(expectedProjection)).Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(controller.Response.Headers.TryGetValue("ETag", out StringValues etag));
        Assert.Equal("\"42\"", etag.ToString());
    }

    /// <summary>
    ///     Verifies that conditional headers do not match a projection with no events.
    /// </summary>
    /// <param name="headerValue">The value of the If-None-Match header field.</param>
    /// <returns>Asynchronous test task.</returns>
    [Theory]
    [InlineData("*")]
    [InlineData("W/\"42\"")]
    public async Task GetAsyncReturnsNotFoundForConditionalRequestsWithoutEvents(
        string headerValue
    )
    {
        // Arrange
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(-1));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock, headerValue);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync returns NotFound when position is NotSet.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsNotFoundWhenPositionIsNotSet()
    {
        // Arrange
        BrookPosition notSetPosition = new(-1);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(notSetPosition);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync returns NotFound when projection is null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsNotFoundWhenProjectionIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(1));
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAsync returns Ok with mapped DTO when projection found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsOkWithMappedDtoWhenProjectionFound()
    {
        // Arrange
        TestProjection expectedProjection = new(42);
        TestDto expectedDto = new("Mapped: 42");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(1));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(expectedProjection)).Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestDto dto = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal("Mapped: 42", dto.Name);
        mapperMock.Verify(m => m.Map(expectedProjection), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync returns NotFound when projection is null.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAtVersionAsyncReturnsNotFoundWhenProjectionIsNull()
    {
        // Arrange
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAtVersionAsync(
            TestEntityId,
            10,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that GetAtVersionAsync returns Ok with mapped DTO when projection found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetAtVersionAsyncReturnsOkWithMappedDtoWhenProjectionFound()
    {
        // Arrange
        const long version = 10;
        TestProjection expectedProjection = new(42);
        TestDto expectedDto = new("Mapped: 42");
        BrookPosition? capturedVersion = null;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .Callback<BrookPosition, CancellationToken>((v, _) => capturedVersion = v)
            .ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(expectedProjection)).Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAtVersionAsync(
            TestEntityId,
            version,
            TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestDto dto = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal("Mapped: 42", dto.Name);
        Assert.NotNull(capturedVersion);
        Assert.Equal(10, capturedVersion.Value.Value);
        mapperMock.Verify(m => m.Map(expectedProjection), Times.Once);
    }

    /// <summary>
    ///     Verifies that GetLatestVersionAsync returns NotFound when position is NotSet.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetLatestVersionAsyncReturnsNotFoundWhenPositionIsNotSet()
    {
        // Arrange
        BrookPosition notSetPosition = new(-1);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(notSetPosition);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<long> result = await controller.GetLatestVersionAsync(
            TestEntityId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    ///     Verifies that GetLatestVersionAsync returns Ok with version when found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task GetLatestVersionAsyncReturnsOkWhenVersionFound()
    {
        // Arrange
        BrookPosition expectedPosition = new(42);
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedPosition);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<long> result = await controller.GetLatestVersionAsync(
            TestEntityId,
            TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        long version = Assert.IsType<long>(okResult.Value);
        Assert.Equal(42L, version);
    }

    /// <summary>
    ///     Verifies that mapper is invoked for GetAtVersionAsync.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task MapperIsInvokedForGetAtVersionAsync()
    {
        // Arrange
        TestProjection capturedProjection = null!;
        TestProjection expectedProjection = new(888);
        TestDto expectedDto = new("Versioned Result");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAtVersionAsync(It.IsAny<BrookPosition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjection);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(It.IsAny<TestProjection>()))
            .Callback<TestProjection>(p => capturedProjection = p)
            .Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        ActionResult<TestDto> result = await controller.GetAtVersionAsync(
            TestEntityId,
            5,
            TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestDto dto = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal("Versioned Result", dto.Name);
        Assert.NotNull(capturedProjection);
        Assert.Equal(888, capturedProjection.Value);
    }

    /// <summary>
    ///     Verifies that mapper is invoked with correct projection.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task MapperIsInvokedWithCorrectProjection()
    {
        // Arrange
        TestProjection capturedProjection = null!;
        TestProjection expectedProjection = new(999);
        TestDto expectedDto = new("Result");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedProjection);
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrookPosition(1));
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(TestEntityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(It.IsAny<TestProjection>()))
            .Callback<TestProjection>(p => capturedProjection = p)
            .Returns(expectedDto);
        TestableControllerWithMapper controller = CreateController(factoryMock, mapperMock);

        // Act
        await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedProjection);
        Assert.Equal(999, capturedProjection.Value);
    }
}