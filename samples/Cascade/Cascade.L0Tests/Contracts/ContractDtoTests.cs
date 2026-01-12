using System;

using Allure.Xunit.Attributes;

using Cascade.Contracts;


namespace Cascade.L0Tests.Contracts;

/// <summary>
///     Tests for the contract DTOs.
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Contracts")]
[AllureSubSuite("DTOs")]
public sealed class ContractDtoTests
{
    /// <summary>
    ///     Verifies BlobItem can be created and properties set.
    /// </summary>
    [Fact]
    [AllureId("web-contracts-004")]
    public void BlobItemCanBeCreated()
    {
        // Arrange & Act
        BlobItem item = new()
        {
            Name = "test-blob",
            Content = "test-content",
        };

        // Assert
        Assert.Equal("test-blob", item.Name);
        Assert.Equal("test-content", item.Content);
    }

    /// <summary>
    ///     Verifies CosmosItem can be created and properties set.
    /// </summary>
    [Fact]
    [AllureId("web-contracts-003")]
    public void CosmosItemCanBeCreated()
    {
        // Arrange & Act
        CosmosItem item = new()
        {
            Id = "test-id",
            Data = "test-data",
        };

        // Assert
        Assert.Equal("test-id", item.Id);
        Assert.Equal("test-data", item.Data);
    }

    /// <summary>
    ///     Verifies EchoResponse can be created and properties set.
    /// </summary>
    [Fact]
    [AllureId("web-contracts-002")]
    public void EchoResponseCanBeCreated()
    {
        // Arrange & Act
        EchoResponse response = new()
        {
            Message = "Hello World",
            ReceivedAt = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc),
        };

        // Assert
        Assert.Equal("Hello World", response.Message);
        Assert.Equal(new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), response.ReceivedAt);
    }

    /// <summary>
    ///     Verifies HealthResponse can be created and properties set.
    /// </summary>
    [Fact]
    [AllureId("web-contracts-001")]
    public void HealthResponseCanBeCreated()
    {
        // Arrange & Act
        HealthResponse response = new()
        {
            Status = "Healthy",
            Timestamp = new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc),
        };

        // Assert
        Assert.Equal("Healthy", response.Status);
        Assert.Equal(new(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), response.Timestamp);
    }
}