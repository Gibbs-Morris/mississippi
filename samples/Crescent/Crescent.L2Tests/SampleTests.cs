namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Sample tests to verify the Crescent test infrastructure is working.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class SampleTests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SampleTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public SampleTests(
        CrescentFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies Blob storage connection string is available.
    /// </summary>
    [Fact]
    public void BlobConnectionStringShouldBeAvailable()
    {
        // Assert
        Assert.False(
            string.IsNullOrEmpty(fixture.BlobConnectionString),
            "the Azurite emulator should provide a connection string");
    }

    /// <summary>
    ///     Verifies Cosmos DB connection string is available.
    /// </summary>
    [Fact]
    public void CosmosConnectionStringShouldBeAvailable()
    {
        // Assert
        Assert.False(
            string.IsNullOrEmpty(fixture.CosmosConnectionString),
            "the Cosmos DB emulator should provide a connection string");
        Assert.Contains("AccountEndpoint=", fixture.CosmosConnectionString, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the Crescent fixture initializes successfully with all resources.
    /// </summary>
    [Fact]
    public void FixtureShouldBeInitialized()
    {
        // Assert
        Assert.True(fixture.IsInitialized, "the Crescent AppHost should start successfully with all emulators");
        Assert.Null(fixture.InitializationError);
    }
}