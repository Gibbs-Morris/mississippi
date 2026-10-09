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
///     Tests wildcard existence through the public controller endpoint; public for xUnit discovery.
/// </summary>
public sealed class UxProjectionControllerWildcardExistenceTests
{
    /// <summary>
    ///     Verifies missing and existing representations for wildcard and normal request controls.
    /// </summary>
    /// <param name="headerValue">The conditional header value.</param>
    /// <param name="hasProjection">Whether the current projection exists.</param>
    /// <param name="expectedStatus">The expected HTTP result status.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData("*", false, StatusCodes.Status404NotFound)]
    [InlineData(" \t* \t", false, StatusCodes.Status404NotFound)]
    [InlineData("*", true, StatusCodes.Status304NotModified)]
    [InlineData(" \t* \t", true, StatusCodes.Status304NotModified)]
    [InlineData("", false, StatusCodes.Status404NotFound)]
    [InlineData("\"43\"", false, StatusCodes.Status404NotFound)]
    [InlineData("", true, StatusCodes.Status200OK)]
    [InlineData("\"43\"", true, StatusCodes.Status200OK)]
    public async Task GetAsyncRespectsCurrentRepresentationExistence(
        string headerValue,
        bool hasProjection,
        int expectedStatus
    )
    {
        // Arrange
        const string entityId = "wildcard-existence-123";
        TestProjection projection = new(100);
        TestDto dto = new("Mapped: 100");
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(42));
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(hasProjection ? projection : null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(entityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        mapperMock.Setup(m => m.Map(projection)).Returns(dto);
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
        controller.Request.Headers.IfNoneMatch = headerValue;

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(entityId, TestContext.Current.CancellationToken);

        // Assert
        if (expectedStatus == StatusCodes.Status404NotFound)
        {
            Assert.IsType<NotFoundResult>(result.Result);
        }
        else if (expectedStatus == StatusCodes.Status304NotModified)
        {
            StatusCodeResult statusCode = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(expectedStatus, statusCode.StatusCode);
        }
        else
        {
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, okResult.Value);
            Assert.Equal("\"42\"", controller.Response.Headers.ETag.ToString());
        }

        mapperMock.Verify(
            m => m.Map(It.IsAny<TestProjection>()),
            expectedStatus == StatusCodes.Status200OK ? Times.Once : Times.Never);
    }

    /// <summary>
    ///     Verifies newly supported matching tag forms preserve missing-state responses.
    /// </summary>
    /// <param name="headerValues">The conditional header field values.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData("W/\"42\"")]
    [InlineData("\"41\", \"42\"")]
    [InlineData("\"42\", \"43\"")]
    [InlineData("\"41\"", "W/\"42\"")]
    [InlineData(" \t\"42\" \t")]
    [InlineData("", "\"42\"", "")]
    public async Task GetAsyncReturnsNotFoundForNewMatchingTagFormsWhenProjectionIsNull(
        params string[] headerValues
    )
    {
        // Arrange
        const string entityId = "matching-tag-missing-state-123";
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new();
        grainMock.Setup(g => g.GetLatestVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(42));
        grainMock.Setup(g => g.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(entityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new();
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
        controller.Request.Headers.IfNoneMatch = new(headerValues);

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(entityId, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }
}