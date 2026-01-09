// <copyright file="SampleTests.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

namespace Crescent.Aspire.L2Tests;

/// <summary>
///     Sample tests to verify the Aspire test infrastructure is working.
/// </summary>
[Collection(AspireTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class SampleTests
#pragma warning restore CA1515
{
    private readonly AspireFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SampleTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public SampleTests(AspireFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    ///     Verifies the Aspire fixture initializes successfully with all resources.
    /// </summary>
    [Fact]
    public void FixtureShouldBeInitialized()
    {
        // Assert
        fixture.IsInitialized.Should().BeTrue(
            because: "the Aspire AppHost should start successfully with all emulators");
        fixture.InitializationError.Should().BeNull(
            because: "there should be no initialization errors");
    }

    /// <summary>
    ///     Verifies Cosmos DB connection string is available.
    /// </summary>
    [Fact]
    public void CosmosConnectionStringShouldBeAvailable()
    {
        // Assert
        fixture.CosmosConnectionString.Should().NotBeNullOrEmpty(
            because: "the Cosmos DB emulator should provide a connection string");
        fixture.CosmosConnectionString.Should().Contain(
            "AccountEndpoint=",
            because: "the connection string should contain an account endpoint");
    }

    /// <summary>
    ///     Verifies Blob storage connection string is available.
    /// </summary>
    [Fact]
    public void BlobConnectionStringShouldBeAvailable()
    {
        // Assert
        fixture.BlobConnectionString.Should().NotBeNullOrEmpty(
            because: "the Azurite emulator should provide a connection string");
    }
}
