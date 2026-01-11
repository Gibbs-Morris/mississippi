using System;

using Allure.Xunit.Attributes;

using Cascade.Web.Contracts.Grains;

using Xunit;


namespace Cascade.Web.L0Tests.Grains;

/// <summary>
///     Tests for the grain contract DTOs.
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Grains")]
[AllureSubSuite("Contracts")]
public sealed class GrainContractTests
{
    /// <summary>
    ///     Verifies GreetResponse can be created with required properties.
    /// </summary>
    [Fact]
    [AllureId("web-grains-001")]
    public void GreetResponseCanBeCreated()
    {
        // Arrange & Act
        DateTime timestamp = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc);
        GreetResponse response = new()
        {
            Greeting = "Hello, World!",
            UppercaseName = "WORLD",
            GeneratedAt = timestamp,
        };

        // Assert
        Assert.Equal("Hello, World!", response.Greeting);
        Assert.Equal("WORLD", response.UppercaseName);
        Assert.Equal(timestamp, response.GeneratedAt);
    }

    /// <summary>
    ///     Verifies GreetResponse record equality works correctly.
    /// </summary>
    [Fact]
    [AllureId("web-grains-002")]
    public void GreetResponseRecordEqualityWorks()
    {
        // Arrange
        DateTime timestamp = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc);
        GreetResponse response1 = new()
        {
            Greeting = "Hello, World!",
            UppercaseName = "WORLD",
            GeneratedAt = timestamp,
        };
        GreetResponse response2 = new()
        {
            Greeting = "Hello, World!",
            UppercaseName = "WORLD",
            GeneratedAt = timestamp,
        };

        // Act & Assert
        Assert.Equal(response1, response2);
    }
}
