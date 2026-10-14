using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Mississippi.Brooks.Abstractions;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;

using MississippiTests.DomainModeling.Gateway.L0Tests;

using Moq;


namespace Mississippi.DomainModeling.Gateway.L0Tests;

/// <summary>
///     Tests captured-version HTTP responses; public for xUnit discovery.
/// </summary>
public sealed class UxProjectionControllerVersionPairingTests
{
    /// <summary>
    ///     Verifies a cursor advance cannot change the state paired with the captured ETag.
    /// </summary>
    /// <param name="capturedVersion">The cursor position selected before the advance.</param>
    /// <param name="headerValue">The conditional header value.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData(0, "")]
    [InlineData(42, "")]
    [InlineData(42, "\"41\"")]
    [InlineData(42, "W/\"41\"")]
    [InlineData(42, "\"41\", \"43\"")]
    [InlineData(42, "malformed")]
    public async Task GetAsyncKeepsCapturedVersionAndMappedDataTogetherWhenCursorAdvances(
        int capturedVersion,
        string headerValue
    )
    {
        // Arrange: completing the cursor read advances the moving-latest source.
        const string entityId = "version-pairing-123";
        int currentVersion = capturedVersion;
        CancellationToken token = TestContext.Current.CancellationToken;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new(MockBehavior.Strict);
        grainMock.Setup(g => g.GetLatestVersionAsync(token))
            .Returns(() =>
            {
                BrookPosition selected = new(currentVersion);
                currentVersion++;
                return new(selected);
            });
        grainMock.Setup(g => g.GetAsync(token)).Returns(() => new(new TestProjection(currentVersion)));
        grainMock.Setup(g => g.GetAtVersionAsync(new(capturedVersion), token))
            .ReturnsAsync(new TestProjection(capturedVersion));
        Mock<IUxProjectionGrainFactory> factoryMock = new(MockBehavior.Strict);
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(entityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new(MockBehavior.Strict);
        mapperMock.Setup(m => m.Map(It.IsAny<TestProjection>()))
            .Returns<TestProjection>(projection => new($"Mapped: {projection.Value}"));
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
        controller.Request.Headers.IfNoneMatch = headerValue;

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(entityId, token);

        // Assert: the advance occurred, but the DTO still belongs to the captured version.
        Assert.Equal(capturedVersion + 1, currentVersion);
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        TestDto dto = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal($"\"{capturedVersion}\"", controller.Response.Headers.ETag.ToString());
        Assert.Equal($"Mapped: {capturedVersion}", dto.Name);
        Assert.Equal("private, must-revalidate", controller.Response.Headers.CacheControl.ToString());
        grainMock.Verify(g => g.GetLatestVersionAsync(token), Times.Once);
        grainMock.Verify(g => g.GetAtVersionAsync(new(capturedVersion), token), Times.Once);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.Is<TestProjection>(p => p.Value == capturedVersion)), Times.Once);
    }

    /// <summary>
    ///     Verifies existence is checked at the captured version when a later representation appears.
    /// </summary>
    /// <param name="headerValue">The conditional header value.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("W/\"42\"")]
    [InlineData("\"41\", \"42\"")]
    public async Task GetAsyncReturnsNotFoundWhenCapturedSnapshotIsMissingDespiteCursorAdvance(
        string headerValue
    )
    {
        // Arrange
        const string entityId = "missing-captured-version-123";
        bool hasAdvanced = false;
        CancellationToken token = TestContext.Current.CancellationToken;
        Mock<IUxProjectionGrain<TestProjection>> grainMock = new(MockBehavior.Strict);
        grainMock.Setup(g => g.GetLatestVersionAsync(token))
            .Returns(() =>
            {
                hasAdvanced = true;
                return new(new BrookPosition(42));
            });
        grainMock.Setup(g => g.GetAsync(token)).Returns(() => new(hasAdvanced ? new TestProjection(43) : null));
        grainMock.Setup(g => g.GetAtVersionAsync(new(42), token)).ReturnsAsync((TestProjection?)null);
        Mock<IUxProjectionGrainFactory> factoryMock = new(MockBehavior.Strict);
        factoryMock.Setup(f => f.GetUxProjectionGrain<TestProjection>(entityId)).Returns(grainMock.Object);
        Mock<IMapper<TestProjection, TestDto>> mapperMock = new(MockBehavior.Strict);
        mapperMock.Setup(m => m.Map(It.IsAny<TestProjection>())).Returns(new TestDto("Newer representation"));
        UxProjectionControllerTestController controller = new(factoryMock.Object, mapperMock.Object);
        controller.Request.Headers.IfNoneMatch = headerValue;

        // Act
        ActionResult<TestDto> result = await controller.GetAsync(entityId, token);

        // Assert
        Assert.True(hasAdvanced);
        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(0, controller.Response.Headers.ETag.Count);
        grainMock.Verify(g => g.GetLatestVersionAsync(token), Times.Once);
        grainMock.Verify(g => g.GetAtVersionAsync(new(42), token), Times.Once);
        grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
    }
}