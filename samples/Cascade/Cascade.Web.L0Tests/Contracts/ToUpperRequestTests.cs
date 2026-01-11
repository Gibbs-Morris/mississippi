using Allure.Xunit.Attributes;

using Cascade.Web.Contracts;

using Xunit;


namespace Cascade.Web.L0Tests.Contracts;

/// <summary>
///     Tests for the ToUpperRequest DTO.
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Contracts")]
[AllureSubSuite("Requests")]
public sealed class ToUpperRequestTests
{
    /// <summary>
    ///     Verifies ToUpperRequest can be created with required properties.
    /// </summary>
    [Fact]
    [AllureId("web-contracts-010")]
    public void ToUpperRequestCanBeCreated()
    {
        // Arrange & Act
        ToUpperRequest request = new()
        {
            Input = "hello world",
        };

        // Assert
        Assert.Equal("hello world", request.Input);
    }
}
