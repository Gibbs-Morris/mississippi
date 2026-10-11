using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Mississippi.Brooks.Abstractions;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Gateway.L0Tests;

using Moq;


namespace MississippiTests.DomainModeling.Gateway.L0Tests;

/// <summary>
///     Tests that conditional header parsing preserves full-body responses for invalid conditions.
/// </summary>
public sealed class UxProjectionControllerHeaderValidationTests
{
    private const string TestEntityId = "header-validation-123";

    /// <summary>
    ///     Verifies that malformed conditions and a quoted opaque asterisk return the current DTO.
    /// </summary>
    /// <param name="headerValues">The conditional header field values.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData("garbage \"42\"")]
    [InlineData("\"42\", garbage")]
    [InlineData("*, \"42\"")]
    [InlineData("\"42\", *")]
    [InlineData("*", "\"42\"")]
    [InlineData("*,")]
    [InlineData("\"*\"")]
    public async Task GetAsyncReturnsBodyForInvalidOrNonmatchingConditions(
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
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
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
    ///     Verifies that a bare wildcard surrounded by HTTP whitespace still matches.
    /// </summary>
    /// <returns>An asynchronous test task.</returns>
    [Fact]
    public async Task GetAsyncReturnsNotModifiedForWildcardWithWhitespace()
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
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
        controller.Request.Headers.IfNoneMatch = " \t* \t";

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(TestEntityId, TestContext.Current.CancellationToken);

        // Assert
        StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Null(result.Value);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }
}