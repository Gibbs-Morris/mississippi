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
///     Tests the entity-tag grammar used for conditional projection requests.
/// </summary>
public sealed class UxProjectionControllerEntityTagGrammarTests
{
    private const string TestEntityId = "entity-tag-grammar-123";

    /// <summary>
    ///     Constructs malformed UTF-16 at runtime so attribute string encoding cannot replace it.
    /// </summary>
    /// <param name="codeUnit">The unpaired surrogate code unit.</param>
    /// <param name="length">The number of repeated unpaired code units.</param>
    /// <param name="isMatchFirst">Whether the matching tag precedes the malformed field.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData(0xD800, 1, false)]
    [InlineData(0xDC00, 1, true)]
    [InlineData(0xD800, 2, false)]
    public async Task GetAsyncRejectsUnpairedSurrogates(
        int codeUnit,
        int length,
        bool isMatchFirst
    )
    {
        char surrogate = (char)codeUnit;
        Assert.True(char.IsSurrogate(surrogate));
        string invalidTag = "\"bad" + new string(surrogate, length) + "value\"";
        string[] headerValues = isMatchFirst ? ["\"42\"", invalidTag] : [invalidTag, "\"42\""];
        await GetAsyncUsesEntityTagGrammar(false, headerValues);
    }

    /// <summary>
    ///     Verifies exact weak prefixes, opaque-tag characters, and complete list validation.
    /// </summary>
    /// <param name="isNotModified">Whether the condition is valid and matches the current version.</param>
    /// <param name="headerValues">The conditional header field values.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData(false, "w/\"42\"")]
    [InlineData(false, "W/ \"42\"")]
    [InlineData(false, "W/\t\"42\"")]
    [InlineData(false, "w/\"other\", \"42\"")]
    [InlineData(false, "\"42\", W/ \"other\"")]
    [InlineData(false, "w/\"other\"", "\"42\"")]
    [InlineData(false, "\"42\"", "W/\t\"other\"")]
    [InlineData(false, "W /\"42\"")]
    [InlineData(false, "W/")]
    [InlineData(false, "W/\"42")]
    [InlineData(false, "W/\"42\"tail")]
    [InlineData(false, "\"bad value\", \"42\"")]
    [InlineData(false, "\"42\", \"bad\tvalue\"")]
    [InlineData(false, "\"bad\u007Fvalue\"", "\"42\"")]
    [InlineData(true, "\"42\", \"bad\u0100value\"")]
    [InlineData(false, "\"bad\u0000value\", \"42\"")]
    [InlineData(false, "\"42\" \"other\"")]
    [InlineData(false, "\"42\", \"unterminated")]
    [InlineData(false, "W/\"43\"")]
    [InlineData(false, "\"42\\\"")]
    [InlineData(false, " , , \t")]
    [InlineData(true, "W/\"42\"")]
    [InlineData(true, " \tW/\"42\" \t")]
    [InlineData(true, "\"other\\\", \"42\"")]
    [InlineData(true, "\"42\", W/\"other\\\"")]
    [InlineData(true, "\"other\\\"", "W/\"42\"")]
    [InlineData(true, "\"\", W/\"42\"")]
    [InlineData(true, "\"!#~\u0080\u00FF\", \"42\"")]
    [InlineData(true, "\"w/42\", \"42\"")]
    [InlineData(true, "\"W/42\", W/\"42\"")]
    [InlineData(true, "\"other,42\", \"42\"")]
    [InlineData(true, " , , W/\"42\",, \t")]
    [InlineData(true, "", "\"42\"", "")]
    [InlineData(true, "\"\u03BB\", \"42\"")]
    [InlineData(true, "\"42\", W/\"\U0001F4A1\"")]
    [InlineData(true, "\"\U00010000\"", "W/\"42\"")]
    [InlineData(true, "\"bad\uD7FFvalue\", \"42\"")]
    [InlineData(true, "\"bad\uE000value\", \"42\"")]
    public async Task GetAsyncUsesEntityTagGrammar(
        bool isNotModified,
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
        if (isNotModified)
        {
            StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
            Assert.Null(result.Value);
            grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
            mapperMock.Verify(m => m.Map(It.IsAny<TestProjection>()), Times.Never);
        }
        else
        {
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, okResult.Value);
            Assert.Equal("\"42\"", controller.Response.Headers.ETag.ToString());
            Assert.Equal("private, must-revalidate", controller.Response.Headers.CacheControl.ToString());
            grainMock.Verify(g => g.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
            mapperMock.Verify(m => m.Map(projection), Times.Once);
        }
    }
}