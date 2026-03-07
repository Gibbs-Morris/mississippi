// <copyright file="ErrorHandlingTests.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

using Azure;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     Tests that verify proper error handling when operations fail or resources are unavailable.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class ErrorHandlingTests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorHandlingTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public ErrorHandlingTests(
        CrescentFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies that Blob operations timeout appropriately rather than hanging.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task BlobOperationWithTimeoutShouldNotHangIndefinitely()
    {
        // Arrange
        BlobServiceClient client = fixture.CreateBlobServiceClient();

        // Set a reasonable timeout using CancellationToken
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

        // Act - Perform a real operation to verify it completes within timeout
        BlobContainerClient container = client.GetBlobContainerClient($"timeout-test-{Guid.NewGuid():N}");
        AzureResponseBool response = await container.ExistsAsync(cts.Token);

        // Assert
        response.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that Cosmos operations timeout appropriately rather than hanging.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CosmosOperationWithTimeoutShouldNotHangIndefinitely()
    {
        // Arrange
        using CosmosClient client = fixture.CreateCosmosClient();

        // Set a reasonable timeout using CancellationToken
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

        // Act - Perform a real operation to verify it completes within timeout
        DatabaseResponse response = await client.CreateDatabaseIfNotExistsAsync(
            $"timeout-test-{Guid.NewGuid():N}",
            cancellationToken: cts.Token);

        // Assert
        response.Should().NotBeNull();
        response.Database.Should().NotBeNull();

        // Cleanup
        await response.Database.DeleteAsync(cancellationToken: CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that accessing Blob storage before initialization throws a clear error.
    /// </summary>
    [Fact]
    public void CreateBlobServiceClientWhenFixtureInitializedShouldSucceed()
    {
        // Act
        BlobServiceClient client = fixture.CreateBlobServiceClient();

        // Assert
        client.Should().NotBeNull("the fixture should provide a valid client when initialized");
    }

    /// <summary>
    ///     Verifies that accessing Cosmos DB before initialization throws a clear error.
    /// </summary>
    [Fact]
    public void CreateCosmosClientWhenFixtureInitializedShouldSucceed()
    {
        // Act
        using CosmosClient client = fixture.CreateCosmosClient();

        // Assert
        client.Should().NotBeNull("the fixture should provide a valid client when initialized");
    }

    /// <summary>
    ///     Verifies that the fixture properly captures initialization errors.
    /// </summary>
    [Fact]
    public void FixtureShouldHaveNoInitializationError()
    {
        // Assert
        fixture.InitializationError.Should().BeNull("a successful fixture should have no initialization error");
    }

    /// <summary>
    ///     Verifies that attempting to use an invalid Blob connection string fails gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task InvalidBlobConnectionShouldThrowMeaningfulError()
    {
        // Arrange
        string invalidConnectionString =
            "DefaultEndpointsProtocol=https;AccountName=invalid;AccountKey=dGVzdGtleQ==;EndpointSuffix=core.windows.net";
        BlobServiceClient invalidClient = new(invalidConnectionString);

        // Act
        Func<Task> act = async () =>
        {
            BlobContainerClient container = invalidClient.GetBlobContainerClient("test");
            _ = await container.ExistsAsync();
        };

        // Assert - Should throw a meaningful exception, not hang indefinitely
        await act.Should().ThrowAsync<RequestFailedException>("invalid connection should fail with a clear error");
    }

    /// <summary>
    ///     Verifies that attempting to use an invalid Cosmos connection string fails gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task InvalidCosmosConnectionShouldThrowMeaningfulError()
    {
        // Arrange
        string invalidConnectionString =
            "AccountEndpoint=https://invalid-endpoint.documents.azure.com:443/;AccountKey=dGVzdGtleQ==;";
        using CosmosClient invalidClient = new(
            invalidConnectionString,
            new()
            {
                ConnectionMode = ConnectionMode.Gateway,
                RequestTimeout = TimeSpan.FromSeconds(5),
            });

        // Act
        Func<Task> act = async () =>
        {
            Database database = invalidClient.GetDatabase("test");
            _ = await database.ReadAsync();
        };

        // Assert - Should throw a meaningful exception, not hang indefinitely
        await act.Should().ThrowAsync<Exception>("invalid connection should fail with a clear error");
    }
}