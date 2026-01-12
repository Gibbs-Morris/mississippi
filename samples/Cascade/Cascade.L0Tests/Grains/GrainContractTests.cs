using System;

using Allure.Xunit.Attributes;

using Cascade.Grains.Abstractions;


namespace Cascade.L0Tests.Grains;

/// <summary>
///     Tests for the grain contract DTOs.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Grains")]
[AllureSubSuite("Contracts")]
public sealed class GrainContractTests
{
    /// <summary>
    ///     Verifies GreetResult can be created with required properties.
    /// </summary>
    [Fact]
    [AllureId("web-grains-001")]
    public void GreetResultCanBeCreated()
    {
        // Arrange & Act
        DateTime timestamp = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc);
        GreetResult response = new()
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
    ///     Verifies GreetResult record equality works correctly.
    /// </summary>
    [Fact]
    [AllureId("web-grains-002")]
    public void GreetResultRecordEqualityWorks()
    {
        // Arrange
        DateTime timestamp = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc);
        GreetResult response1 = new()
        {
            Greeting = "Hello, World!",
            UppercaseName = "WORLD",
            GeneratedAt = timestamp,
        };
        GreetResult response2 = new()
        {
            Greeting = "Hello, World!",
            UppercaseName = "WORLD",
            GeneratedAt = timestamp,
        };

        // Act & Assert
        Assert.Equal(response1, response2);
    }
}