// <copyright file="CosmosDbTests.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

namespace Crescent.Aspire.L2Tests;

/// <summary>
///     Integration tests for Cosmos DB operations using the Aspire emulator.
/// </summary>
[Collection(AspireTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class CosmosDbTests : IAsyncDisposable
#pragma warning restore CA1515
{
    private readonly CosmosClient cosmosClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosDbTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public CosmosDbTests(AspireFixture fixture)
    {
        Console.WriteLine("=== CosmosDbTests CONSTRUCTOR ===");
        ArgumentNullException.ThrowIfNull(fixture);
        Console.WriteLine("[CosmosDbTests] Fixture received, creating CosmosClient...");
        cosmosClient = fixture.CreateCosmosClient();
        Console.WriteLine($"[CosmosDbTests] CosmosClient created, endpoint: {cosmosClient.Endpoint}");
        Console.WriteLine("=== END CosmosDbTests CONSTRUCTOR ===");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        cosmosClient.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    ///     Verifies that a document can be written to Cosmos DB.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task WriteDocumentShouldSucceed()
    {
        // Arrange
        string testId = Guid.NewGuid().ToString();
        TestDocument document = new()
        {
            Id = testId,
            Name = "Test Document",
            Value = 42,
            CreatedAt = DateTime.UtcNow,
        };

        Container container = await GetOrCreateContainerAsync();

        // Act
        ItemResponse<TestDocument> response = await container.CreateItemAsync(
            document,
            new PartitionKey(testId));

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            because: "the document should be created successfully");
        response.Resource.Should().NotBeNull();
        response.Resource.Id.Should().Be(testId);
        response.Resource.Name.Should().Be("Test Document");
    }

    /// <summary>
    ///     Verifies that a document can be read from Cosmos DB.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReadDocumentShouldReturnWrittenDocument()
    {
        // Arrange
        string testId = Guid.NewGuid().ToString();
        TestDocument document = new()
        {
            Id = testId,
            Name = "Read Test Document",
            Value = 123,
            CreatedAt = DateTime.UtcNow,
        };

        Container container = await GetOrCreateContainerAsync();

        // Write the document first
        await container.CreateItemAsync(
            document,
            new PartitionKey(testId));

        // Act
        ItemResponse<TestDocument> readResponse = await container.ReadItemAsync<TestDocument>(
            testId,
            new PartitionKey(testId));

        // Assert
        readResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: "the document should be read successfully");
        readResponse.Resource.Should().NotBeNull();
        readResponse.Resource.Id.Should().Be(testId);
        readResponse.Resource.Name.Should().Be("Read Test Document");
        readResponse.Resource.Value.Should().Be(123);
    }

    /// <summary>
    ///     Verifies that reading a non-existent document throws an appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReadNonExistentDocumentShouldThrowNotFoundException()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        Container container = await GetOrCreateContainerAsync();

        // Act
        Func<Task> act = async () => await container.ReadItemAsync<TestDocument>(
            nonExistentId,
            new PartitionKey(nonExistentId));

        // Assert
        await act.Should()
            .ThrowAsync<CosmosException>(because: "the document does not exist")
            .Where(e => e.StatusCode == HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Verifies that documents can be queried from Cosmos DB.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task QueryDocumentsShouldReturnMatchingDocuments()
    {
        // Arrange
        string uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        Container container = await GetOrCreateContainerAsync();

        // Write multiple documents
        for (int i = 0; i < 3; i++)
        {
            TestDocument doc = new()
            {
                Id = $"{uniquePrefix}-{i}",
                Name = $"Query Test {uniquePrefix}",
                Value = i * 10,
                CreatedAt = DateTime.UtcNow,
            };
            await container.CreateItemAsync(
                doc,
                new PartitionKey(doc.Id));
        }

        // Act
        string query = $"SELECT * FROM c WHERE STARTSWITH(c.name, 'Query Test {uniquePrefix}')";
        List<TestDocument> results = new();

        using FeedIterator<TestDocument> iterator = container.GetItemQueryIterator<TestDocument>(query);
        while (iterator.HasMoreResults)
        {
            FeedResponse<TestDocument> batch = await iterator.ReadNextAsync();
            results.AddRange(batch);
        }

        // Assert
        results.Should().HaveCount(
            3,
            because: "we created 3 documents with the matching prefix");
        results.Should().AllSatisfy(doc =>
        {
            doc.Name.Should().StartWith($"Query Test {uniquePrefix}");
        });
    }

    /// <summary>
    ///     Gets or creates the test container, ensuring the database exists.
    ///     Uses retry with timeout to handle emulator startup race conditions.
    /// </summary>
    private async Task<Container> GetOrCreateContainerAsync()
    {
        Console.WriteLine("=== GetOrCreateContainerAsync DEBUG ===");
        Console.WriteLine($"[GetOrCreateContainerAsync] Starting, endpoint: {cosmosClient.Endpoint}");

        // Retry up to 10 times with 5s delay (total ~90s wait for emulator readiness)
        const int MaxRetries = 10;
        const int RetryDelayMs = 5000;
        const int TimeoutSeconds = 30;

        Database? database = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Console.WriteLine($"[GetOrCreateContainerAsync] Attempt {attempt}/{MaxRetries} to create database 'testdb'...");
            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(TimeoutSeconds));
                database = await cosmosClient.CreateDatabaseIfNotExistsAsync("testdb", cancellationToken: cts.Token);
                Console.WriteLine($"[GetOrCreateContainerAsync] Database obtained: {database.Id}");
                break;
            }
            catch (OperationCanceledException) when (attempt < MaxRetries)
            {
                Console.WriteLine($"[GetOrCreateContainerAsync] Timeout after {TimeoutSeconds}s, retrying in {RetryDelayMs / 1000}s...");
                await Task.Delay(RetryDelayMs);
            }
            catch (CosmosException ex) when (attempt < MaxRetries && ex.StatusCode >= HttpStatusCode.InternalServerError)
            {
                Console.WriteLine($"[GetOrCreateContainerAsync] Cosmos error {ex.StatusCode}: {ex.Message}, retrying in {RetryDelayMs / 1000}s...");
                await Task.Delay(RetryDelayMs);
            }
        }

        if (database is null)
        {
            throw new InvalidOperationException("Failed to create database after maximum retries");
        }

        Console.WriteLine("[GetOrCreateContainerAsync] Creating container 'testcontainer'...");
        using CancellationTokenSource containerCts = new(TimeSpan.FromSeconds(TimeoutSeconds));
        ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(
            "testcontainer",
            "/id",
            throughput: 400,
            cancellationToken: containerCts.Token);
        Console.WriteLine($"[GetOrCreateContainerAsync] Container obtained: {containerResponse.Container.Id}, StatusCode: {containerResponse.StatusCode}");
        Console.WriteLine("=== END GetOrCreateContainerAsync DEBUG ===");

        return containerResponse.Container;
    }

    /// <summary>
    ///     Test document model for Cosmos DB operations.
    /// </summary>
    /// <remarks>
    ///     The Cosmos SDK uses Newtonsoft.Json by default, so we use
    ///     <see cref="Newtonsoft.Json.JsonPropertyAttribute"/> for serialization.
    /// </remarks>
    private sealed class TestDocument
    {
        /// <summary>
        ///     Gets or sets the document ID.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the document name.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a numeric value.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("value")]
        public int Value { get; set; }

        /// <summary>
        ///     Gets or sets the creation timestamp.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
