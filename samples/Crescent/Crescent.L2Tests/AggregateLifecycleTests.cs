using Crescent.L2Tests.Domain.Counter;
using Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.L2Tests.Domain.CounterSummary;

using FluentAssertions;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Xunit;
using Xunit.Abstractions;

namespace Crescent.L2Tests;

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
    private static string NewEntityId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

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
        initResult.Success.Should().BeTrue("Initialize should succeed");
        output.WriteLine("[Test] Initialize() succeeded");

        // Act - Increment 10 times
        for (int i = 0; i < 10; i++)
        {
            OperationResult incResult = await counter.ExecuteAsync(new IncrementCounter());
            incResult.Success.Should().BeTrue($"Increment[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Increment x10 succeeded");

        // Act - Decrement 5 times
        for (int i = 0; i < 5; i++)
        {
            OperationResult decResult = await counter.ExecuteAsync(new DecrementCounter());
            decResult.Success.Should().BeTrue($"Decrement[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Decrement x5 succeeded");

        // Act - Reset to 100
        OperationResult resetResult = await counter.ExecuteAsync(new ResetCounter { NewValue = 100 });
        resetResult.Success.Should().BeTrue("Reset should succeed");
        output.WriteLine("[Test] Reset(100) succeeded");

        // Act - Increment 3 more times by different amounts
        for (int i = 1; i <= 3; i++)
        {
            OperationResult incResult = await counter.ExecuteAsync(new IncrementCounter { Amount = i * 10 });
            incResult.Success.Should().BeTrue($"Increment({i * 10}) should succeed");
        }

        output.WriteLine("[Test] Increment by 10, 20, 30 succeeded");

        // Assert - Verify projection
        // Expected: 100 (reset) + 10 + 20 + 30 = 160
        // Operations: 1 init + 10 inc + 5 dec + 1 reset + 3 inc = 20
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);

        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(160, "Final count should be 100 + 10 + 20 + 30 = 160");
        projection.TotalOperations.Should().Be(20, "Operations: 1 + 10 + 5 + 1 + 3 = 20");

        output.WriteLine($"[Test] Projection verified: Count={projection.CurrentCount}, Operations={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: BasicLifecycle completed successfully!");
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
        initResult.Success.Should().BeTrue("Initialize should succeed");

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
        successCount.Should().Be(operationCount, "All operations should succeed");
        output.WriteLine($"[Test] All {operationCount} increment operations completed");

        // Verify projection - add a small delay to allow projection catch-up under heavy emulator load
        await Task.Delay(500);

        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);

        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(operationCount, $"Count should be {operationCount}");
        projection.TotalOperations.Should().Be(operationCount + 1, $"Operations should be {operationCount + 1} (1 init + {operationCount} inc)");

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
        incResultBeforeInit.Success.Should().BeFalse("Increment before init should fail");
        output.WriteLine($"[Test] Increment before init failed as expected: {incResultBeforeInit.ErrorMessage}");

        // Act - Now initialize
        OperationResult initResult = await counter.ExecuteAsync(new InitializeCounter(10));
        initResult.Success.Should().BeTrue("Initialize should succeed");
        output.WriteLine("[Test] Initialize(10) succeeded");

        // Act - Attempt to re-initialize (should fail)
        OperationResult reinitResult = await counter.ExecuteAsync(new InitializeCounter(20));
        reinitResult.Success.Should().BeFalse("Re-initialization should fail");
        reinitResult.ErrorCode.Should().Be(AggregateErrorCodes.AlreadyExists);
        output.WriteLine($"[Test] Re-initialize failed as expected: {reinitResult.ErrorMessage}");

        // Act - Attempt decrement with zero amount (should fail validation)
        OperationResult zeroDecResult = await counter.ExecuteAsync(new DecrementCounter { Amount = 0 });
        zeroDecResult.Success.Should().BeFalse("Decrement(0) should fail validation");
        output.WriteLine($"[Test] Decrement(0) failed as expected: {zeroDecResult.ErrorMessage}");

        // Assert - Projection should only reflect successful operations
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);

        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(10, "Count should be 10 (only init succeeded)");
        projection.TotalOperations.Should().Be(1, "Only 1 successful operation (init)");

        output.WriteLine("[Test] PASSED: Validation errors properly detected!");
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
        initResult.Success.Should().BeTrue("Initialize should succeed");

        // Act - Fire concurrent increment commands
        List<Task<OperationResult>> tasks = [];
        for (int i = 0; i < concurrentOps; i++)
        {
            tasks.Add(counter.ExecuteAsync(new IncrementCounter()));
        }

        OperationResult[] results = await Task.WhenAll(tasks);

        // Assert - All should succeed (Orleans serializes grain calls)
        int successCount = results.Count(r => r.Success);
        successCount.Should().Be(concurrentOps, "All concurrent operations should succeed");

        // Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);

        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(concurrentOps, $"Count should be {concurrentOps}");
        projection.TotalOperations.Should().Be(concurrentOps + 1, $"Operations should be {concurrentOps + 1}");

        output.WriteLine($"[Test] Concurrent completed: {successCount}/{concurrentOps} operations succeeded");
        output.WriteLine("[Test] PASSED: Concurrent commands all succeeded!");
    }
}
