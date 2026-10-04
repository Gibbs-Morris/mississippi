using Newtonsoft.Json;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Integration tests for Cosmos DB operations using the Crescent emulator.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class CosmosDbTests : IAsyncDisposable
#pragma warning restore CA1515
{
    private static readonly DateTime CreatedAtUtc = new(2024, 02, 03, 04, 05, 06, DateTimeKind.Utc);

    private readonly CosmosClient cosmosClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosDbTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public CosmosDbTests(
        CrescentFixture fixture
    )
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
            Console.WriteLine(
                $"[GetOrCreateContainerAsync] Attempt {attempt}/{MaxRetries} to create database 'testdb'...");
            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(TimeoutSeconds));
                database = await cosmosClient.CreateDatabaseIfNotExistsAsync("testdb", cancellationToken: cts.Token);
                Console.WriteLine($"[GetOrCreateContainerAsync] Database obtained: {database.Id}");
                break;
            }
            catch (OperationCanceledException) when (attempt < MaxRetries)
            {
                Console.WriteLine(
                    $"[GetOrCreateContainerAsync] Timeout after {TimeoutSeconds}s, retrying in {RetryDelayMs / 1000}s...");
                await Task.Delay(RetryDelayMs);
            }
            catch (CosmosException ex) when ((attempt < MaxRetries) &&
                                             (ex.StatusCode >= HttpStatusCode.InternalServerError))
            {
                Console.WriteLine(
                    $"[GetOrCreateContainerAsync] Cosmos error {ex.StatusCode}: {ex.Message}, retrying in {RetryDelayMs / 1000}s...");
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
            400,
            cancellationToken: containerCts.Token);
        Console.WriteLine(
            $"[GetOrCreateContainerAsync] Container obtained: {containerResponse.Container.Id}, StatusCode: {containerResponse.StatusCode}");
        Console.WriteLine("=== END GetOrCreateContainerAsync DEBUG ===");
        return containerResponse.Container;
    }

    /// <summary>
    ///     Test document model for Cosmos DB operations.
    /// </summary>
    /// <remarks>
    ///     The Cosmos SDK uses Newtonsoft.Json by default, so we use
    ///     <see cref="Newtonsoft.Json.JsonPropertyAttribute" /> for serialization.
    /// </remarks>
    private sealed class TestDocument
    {
        /// <summary>
        ///     Gets or sets the creation timestamp.
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        ///     Gets or sets the document ID.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the document name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a numeric value.
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; }
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
                CreatedAt = CreatedAtUtc,
            };
            await container.CreateItemAsync(
                doc,
                new PartitionKey(doc.Id),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        // Act
        string query = $"SELECT * FROM c WHERE STARTSWITH(c.name, 'Query Test {uniquePrefix}')";
        List<TestDocument> results = new();
        using FeedIterator<TestDocument> iterator = container.GetItemQueryIterator<TestDocument>(query);
        while (iterator.HasMoreResults)
        {
            FeedResponse<TestDocument> batch = await iterator.ReadNextAsync(TestContext.Current.CancellationToken);
            results.AddRange(batch);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(
            results,
            doc => { Assert.StartsWith($"Query Test {uniquePrefix}", doc.Name, StringComparison.Ordinal); });
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
            CreatedAt = CreatedAtUtc,
        };
        Container container = await GetOrCreateContainerAsync();

        // Write the document first
        await container.CreateItemAsync(
            document,
            new PartitionKey(testId),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        ItemResponse<TestDocument> readResponse = await container.ReadItemAsync<TestDocument>(
            testId,
            new(testId),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.NotNull(readResponse.Resource);
        Assert.Equal(testId, readResponse.Resource.Id);
        Assert.Equal("Read Test Document", readResponse.Resource.Name);
        Assert.Equal(123, readResponse.Resource.Value);
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
        Func<Task> act = async () => await container.ReadItemAsync<TestDocument>(nonExistentId, new(nonExistentId));

        // Assert
        CosmosException exception = await Assert.ThrowsAnyAsync<CosmosException>(act);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
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
            CreatedAt = CreatedAtUtc,
        };
        Container container = await GetOrCreateContainerAsync();

        // Act
        ItemResponse<TestDocument> response = await container.CreateItemAsync(
            document,
            new PartitionKey(testId),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Resource);
        Assert.Equal(testId, response.Resource.Id);
        Assert.Equal("Test Document", response.Resource.Name);
    }
}