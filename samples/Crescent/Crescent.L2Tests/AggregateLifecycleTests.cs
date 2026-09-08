using Mississippi.DomainModeling.Abstractions;

using Xunit.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Tests validating aggregate lifecycle, concurrency, throughput, and validation.
///     Migrated from ConsoleApp AggregateScenario.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class AggregateLifecycleTests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    private readonly ITestOutputHelper output;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateLifecycleTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    /// <param name="output">The xUnit test output helper.</param>
    public AggregateLifecycleTests(
        CrescentFixture fixture,
        ITestOutputHelper output
    )
    {
        this.fixture = fixture;
        this.output = output;
    }

    /// <summary>
    ///     Generates a unique entity ID for test isolation.
    /// </summary>
    private static string NewEntityId(
        string prefix
    ) =>
        $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>
    ///     Tests the basic counter aggregate lifecycle:
    ///     Initialize → Increment (10x) → Decrement (5x) → Reset → Increment (3x).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task BasicLifecycleCompletesSuccessfully()
    {
        // Arrange
        string entityId = NewEntityId("lifecycle");
        output.WriteLine($"[Test] Starting BasicLifecycle with entity ID: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize
        OperationResult initResult = await counter.ExecuteAsync(new InitializeCounter());
        Assert.True(initResult.Success, "Initialize should succeed");
        output.WriteLine("[Test] Initialize() succeeded");

        // Act - Increment 10 times
        for (int i = 0; i < 10; i++)
        {
            OperationResult incResult = await counter.ExecuteAsync(new IncrementCounter());
            Assert.True(incResult.Success, $"Increment[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Increment x10 succeeded");

        // Act - Decrement 5 times
        for (int i = 0; i < 5; i++)
        {
            OperationResult decResult = await counter.ExecuteAsync(new DecrementCounter());
            Assert.True(decResult.Success, $"Decrement[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Decrement x5 succeeded");

        // Act - Reset to 100
        OperationResult resetResult = await counter.ExecuteAsync(
            new ResetCounter
            {
                NewValue = 100,
            });
        Assert.True(resetResult.Success, "Reset should succeed");
        output.WriteLine("[Test] Reset(100) succeeded");

        // Act - Increment 3 more times by different amounts
        for (int i = 1; i <= 3; i++)
        {
            OperationResult incResult = await counter.ExecuteAsync(
                new IncrementCounter
                {
                    Amount = i * 10,
                });
            Assert.True(incResult.Success, $"Increment({i * 10}) should succeed");
        }

        output.WriteLine("[Test] Increment by 10, 20, 30 succeeded");

        // Assert - Verify projection
        // Expected: 100 (reset) + 10 + 20 + 30 = 160
        // Operations: 1 init + 10 inc + 5 dec + 1 reset + 3 inc = 20
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(160, projection.CurrentCount);
        Assert.Equal(20, projection.TotalOperations);
        output.WriteLine(
            $"[Test] Projection verified: Count={projection.CurrentCount}, Operations={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: BasicLifecycle completed successfully!");
    }

    /// <summary>
    ///     Tests concurrency scenario with multiple commands executed in parallel.
    ///     Orleans serializes grain calls so all should succeed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentCommandsAllSucceed()
    {
        // Arrange
        const int concurrentOps = 20;
        string entityId = NewEntityId("concurrent");
        output.WriteLine($"[Test] Starting Concurrent test with {concurrentOps} parallel operations: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize
        OperationResult initResult = await counter.ExecuteAsync(new InitializeCounter());
        Assert.True(initResult.Success, "Initialize should succeed");

        // Act - Fire concurrent increment commands
        List<Task<OperationResult>> tasks = [];
        for (int i = 0; i < concurrentOps; i++)
        {
            tasks.Add(counter.ExecuteAsync(new IncrementCounter()));
        }

        OperationResult[] results = await Task.WhenAll(tasks);

        // Assert - All should succeed (Orleans serializes grain calls)
        int successCount = results.Count(r => r.Success);
        Assert.Equal(concurrentOps, successCount);

        // Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(concurrentOps, projection.CurrentCount);
        Assert.Equal(concurrentOps + 1, projection.TotalOperations);
        output.WriteLine($"[Test] Concurrent completed: {successCount}/{concurrentOps} operations succeeded");
        output.WriteLine("[Test] PASSED: Concurrent commands all succeeded!");
    }

    /// <summary>
    ///     Tests high-throughput scenario with many rapid operations.
    ///     Uses 25 operations as a balance between meaningful throughput testing
    ///     and emulator performance constraints.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ThroughputManyOperationsSucceed()
    {
        // Arrange - 25 operations balances meaningful throughput testing with emulator constraints
        const int operationCount = 25;
        string entityId = NewEntityId("throughput");
        output.WriteLine($"[Test] Starting Throughput test with {operationCount} operations: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize
        OperationResult initResult = await counter.ExecuteAsync(new InitializeCounter());
        Assert.True(initResult.Success, "Initialize should succeed");

        // Act - Run rapid increments
        int successCount = 0;
        for (int i = 0; i < operationCount; i++)
        {
            OperationResult result = await counter.ExecuteAsync(new IncrementCounter());
            if (result.Success)
            {
                successCount++;
            }
        }

        // Assert
        Assert.Equal(operationCount, successCount);
        output.WriteLine($"[Test] All {operationCount} increment operations completed");

        // Verify projection - add a small delay to allow projection catch-up under heavy emulator load
        await Task.Delay(500);
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(operationCount, projection.CurrentCount);
        Assert.Equal(operationCount + 1, projection.TotalOperations);
        output.WriteLine($"[Test] Throughput completed: {successCount}/{operationCount} operations succeeded");
        output.WriteLine("[Test] PASSED: Throughput scenario completed!");
    }

    /// <summary>
    ///     Tests validation errors by attempting invalid operations.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidationErrorsProperlyDetected()
    {
        // Arrange
        string entityId = NewEntityId("validation");
        output.WriteLine($"[Test] Starting Validation test: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Attempt increment before initialization (should fail)
        OperationResult incResultBeforeInit = await counter.ExecuteAsync(new IncrementCounter());
        Assert.False(incResultBeforeInit.Success, "Increment before init should fail");
        output.WriteLine($"[Test] Increment before init failed as expected: {incResultBeforeInit.ErrorMessage}");

        // Act - Now initialize
        OperationResult initResult = await counter.ExecuteAsync(new InitializeCounter(10));
        Assert.True(initResult.Success, "Initialize should succeed");
        output.WriteLine("[Test] Initialize(10) succeeded");

        // Act - Attempt to re-initialize (should fail)
        OperationResult reinitResult = await counter.ExecuteAsync(new InitializeCounter(20));
        Assert.False(reinitResult.Success, "Re-initialization should fail");
        Assert.Equal(AggregateErrorCodes.AlreadyExists, reinitResult.ErrorCode);
        output.WriteLine($"[Test] Re-initialize failed as expected: {reinitResult.ErrorMessage}");

        // Act - Attempt decrement with zero amount (should fail validation)
        OperationResult zeroDecResult = await counter.ExecuteAsync(
            new DecrementCounter
            {
                Amount = 0,
            });
        Assert.False(zeroDecResult.Success, "Decrement(0) should fail validation");
        output.WriteLine($"[Test] Decrement(0) failed as expected: {zeroDecResult.ErrorMessage}");

        // Assert - Projection should only reflect successful operations
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(10, projection.CurrentCount);
        Assert.Equal(1, projection.TotalOperations);
        output.WriteLine("[Test] PASSED: Validation errors properly detected!");
    }
}