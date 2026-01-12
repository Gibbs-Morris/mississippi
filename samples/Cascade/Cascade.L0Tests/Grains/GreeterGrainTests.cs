using System;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Cascade.Grains.Abstractions;
using Cascade.Silo.Grains;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Orleans.Runtime;


namespace Cascade.L0Tests.Grains;

/// <summary>
///     Unit tests for the GreeterGrain implementation.
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Grains")]
[AllureSubSuite("GreeterGrain")]
public sealed class GreeterGrainTests
{
    /// <summary>
    ///     Verifies GreetAsync returns a properly formatted greeting.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureId("web-grains-010")]
    public async Task GreetAsyncReturnsFormattedGreeting()
    {
        // Arrange
        IGrainContext grainContext = Substitute.For<IGrainContext>();
        grainContext.GrainId.Returns(GrainId.Create("greeter", "TestUser"));
        ILogger<GreeterGrain> logger = Substitute.For<ILogger<GreeterGrain>>();
        GreeterGrain grain = new(grainContext, logger);

        // Act
        GreetResult response = await grain.GreetAsync();

        // Assert
        Assert.Equal("Hello, TestUser!", response.Greeting);
        Assert.Equal("TESTUSER", response.UppercaseName);
    }

    /// <summary>
    ///     Verifies ToUpperAsync converts input to uppercase.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureId("web-grains-011")]
    public async Task ToUpperAsyncConvertsToUppercase()
    {
        // Arrange
        IGrainContext grainContext = Substitute.For<IGrainContext>();
        grainContext.GrainId.Returns(GrainId.Create("greeter", "converter"));
        ILogger<GreeterGrain> logger = Substitute.For<ILogger<GreeterGrain>>();
        GreeterGrain grain = new(grainContext, logger);

        // Act
        string result = await grain.ToUpperAsync("hello world");

        // Assert
        Assert.Equal("HELLO WORLD", result);
    }

    /// <summary>
    ///     Verifies ToUpperAsync throws for null or whitespace input.
    /// </summary>
    /// <param name="input">The invalid input to test.</param>
    /// <returns>A task representing the async test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [AllureId("web-grains-012")]
    public async Task ToUpperAsyncThrowsForInvalidInput(
        string? input
    )
    {
        // Arrange
        IGrainContext grainContext = Substitute.For<IGrainContext>();
        grainContext.GrainId.Returns(GrainId.Create("greeter", "converter"));
        ILogger<GreeterGrain> logger = Substitute.For<ILogger<GreeterGrain>>();
        GreeterGrain grain = new(grainContext, logger);

        // Act & Assert - ArgumentNullException (for null) and ArgumentException (for empty/whitespace) are expected
        await Assert.ThrowsAnyAsync<ArgumentException>(() => grain.ToUpperAsync(input!));
    }
}