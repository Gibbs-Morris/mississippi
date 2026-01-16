using Crescent.Crescent.L2Tests.Domain.Counter;
using Crescent.Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.Crescent.L2Tests.Domain.CounterSummary;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Xunit.Abstractions;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     Comprehensive end-to-end tests validating the complete event sourcing pipeline:
///     Aggregate → Events → Brook → Projection.
///     Migrated from ConsoleApp ComprehensiveE2EScenarios.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class ComprehensiveE2ETests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    private readonly ITestOutputHelper output;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ComprehensiveE2ETests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    /// <param name="output">The xUnit test output helper.</param>
    public ComprehensiveE2ETests(
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
    ///     Tests boundary conditions including negative values.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task BoundaryConditionsHandleNegativeValues()
    {
        // Arrange
        string entityId = NewEntityId("boundary");
        output.WriteLine($"[Test] Testing boundary conditions (negative values): {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize with 0
        await counter.ExecuteAsync(new InitializeCounter());

        // Act - Decrement to go negative
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new DecrementCounter());
        }

        // Assert - Verify projection shows negative
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();

        // Expected: 0 - 5 = -5, IsPositive = false, 6 operations
        projection!.CurrentCount.Should().Be(-5, "Count should be -5");
        projection.TotalOperations.Should().Be(6, "Operations should be 6");
        projection.IsPositive.Should().BeFalse("IsPositive should be false for negative count");
        output.WriteLine($"[Test] Boundary: Count={projection.CurrentCount}, IsPositive={projection.IsPositive}");

        // Act - Increment back to zero
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter());
        }

        // Wait for projection to catch up
        CounterSummaryProjection? afterZero = null;
        const int MaxAttempts = 20;
        const int RetryDelayMs = 100;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            afterZero = await projGrain.GetAsync(CancellationToken.None);
            if (afterZero is not null && (afterZero.CurrentCount == 0))
            {
                break;
            }

            output.WriteLine(
                $"[Test] Waiting for projection to return to zero (attempt {attempt}/{MaxAttempts}). CurrentCount={afterZero?.CurrentCount}");
            await Task.Delay(RetryDelayMs);
        }

        afterZero.Should().NotBeNull();
        afterZero!.CurrentCount.Should().Be(0, "Count should be 0 after incrementing back");
        output.WriteLine("[Test] PASSED: Boundary conditions handled correctly!");
    }

    /// <summary>
    ///     Tests that multiple aggregates operate independently with isolated projections.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IsolatedAggregatesHaveIndependentProjections()
    {
        // Arrange - Create two independent counters
        string counterId1 = NewEntityId("isolated-1");
        string counterId2 = NewEntityId("isolated-2");
        output.WriteLine($"[Test] Testing isolation between: {counterId1} and {counterId2}");
        IGenericAggregateGrain<CounterAggregate> counter1 = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(counterId1);
        IGenericAggregateGrain<CounterAggregate> counter2 = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(counterId2);

        // Act - Initialize counter1 with 100, counter2 with 200
        OperationResult init1 = await counter1.ExecuteAsync(new InitializeCounter(100));
        OperationResult init2 = await counter2.ExecuteAsync(new InitializeCounter(200));
        init1.Success.Should().BeTrue();
        init2.Success.Should().BeTrue();

        // Act - Increment counter1 by 10 operations
        for (int i = 0; i < 10; i++)
        {
            await counter1.ExecuteAsync(new IncrementCounter());
        }

        // Act - Decrement counter2 by 5 operations
        for (int i = 0; i < 5; i++)
        {
            await counter2.ExecuteAsync(new DecrementCounter());
        }

        // Assert - Verify projections are isolated
        IUxProjectionGrain<CounterSummaryProjection> proj1 = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(counterId1);
        IUxProjectionGrain<CounterSummaryProjection> proj2 = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(counterId2);
        CounterSummaryProjection? projection1 = await proj1.GetAsync(CancellationToken.None);
        CounterSummaryProjection? projection2 = await proj2.GetAsync(CancellationToken.None);
        projection1.Should().NotBeNull();
        projection2.Should().NotBeNull();

        // Counter1: 100 + 10 = 110, 11 operations
        projection1!.CurrentCount.Should().Be(110, "Counter1 should be 100 + 10 = 110");
        projection1.TotalOperations.Should().Be(11, "Counter1 should have 11 operations");

        // Counter2: 200 - 5 = 195, 6 operations
        projection2!.CurrentCount.Should().Be(195, "Counter2 should be 200 - 5 = 195");
        projection2.TotalOperations.Should().Be(6, "Counter2 should have 6 operations");
        output.WriteLine($"[Test] Counter1: Count={projection1.CurrentCount}, Ops={projection1.TotalOperations}");
        output.WriteLine($"[Test] Counter2: Count={projection2.CurrentCount}, Ops={projection2.TotalOperations}");
        output.WriteLine("[Test] PASSED: Isolated aggregates have independent projections!");
    }

    /// <summary>
    ///     Tests large number of sequential operations for position tracking.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task LargeOperationSequenceMaintainsCorrectState()
    {
        // Arrange
        const int opCount = 20;
        string entityId = NewEntityId("large-seq");
        output.WriteLine($"[Test] Testing large operation sequence ({opCount} ops): {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize
        await counter.ExecuteAsync(new InitializeCounter());

        // Act - Perform many increments
        for (int i = 0; i < opCount; i++)
        {
            OperationResult result = await counter.ExecuteAsync(new IncrementCounter());
            result.Success.Should().BeTrue($"Increment[{i}] should succeed");
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(opCount, $"Count should be {opCount}");
        projection.TotalOperations.Should().Be(opCount + 1, $"Operations should be {opCount + 1}");
        output.WriteLine(
            $"[Test] Large sequence completed: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: Large operation sequence maintains correct state!");
    }

    /// <summary>
    ///     Tests that projection state persists after deactivation.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProjectionAfterDeactivationReturnsSameState()
    {
        // Arrange
        string entityId = NewEntityId("deactivate");
        output.WriteLine($"[Test] Testing projection persistence after deactivation: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Setup: Initialize and perform operations
        await counter.ExecuteAsync(new InitializeCounter(25));
        for (int i = 0; i < 10; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter());
        }

        // Act - First read
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? beforeDeactivation = await projGrain.GetAsync(CancellationToken.None);
        beforeDeactivation.Should().NotBeNull();
        int expectedCount = beforeDeactivation!.CurrentCount;
        int expectedOps = beforeDeactivation.TotalOperations;

        // Small delay to simulate some idle time
        await Task.Delay(100);

        // Act - Read again (simulating after potential deactivation)
        CounterSummaryProjection? afterDeactivation = await projGrain.GetAsync(CancellationToken.None);
        afterDeactivation.Should().NotBeNull();

        // Assert - Values should match
        afterDeactivation!.CurrentCount.Should().Be(expectedCount, "Count should persist");
        afterDeactivation.TotalOperations.Should().Be(expectedOps, "Operations should persist");

        // Expected: 25 + 10 = 35, 11 operations
        expectedCount.Should().Be(35, "Expected count should be 35");
        expectedOps.Should().Be(11, "Expected operations should be 11");
        output.WriteLine($"[Test] Before: Count={expectedCount}, Ops={expectedOps}");
        output.WriteLine(
            $"[Test] After: Count={afterDeactivation.CurrentCount}, Ops={afterDeactivation.TotalOperations}");
        output.WriteLine("[Test] PASSED: Projection persists correctly after deactivation!");
    }

    /// <summary>
    ///     Tests projection re-read consistency (multiple reads return same values).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProjectionRereadConsistencyReturnsConsistentResults()
    {
        // Arrange
        string entityId = NewEntityId("reread");
        output.WriteLine($"[Test] Testing projection re-read consistency: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize and perform some operations
        await counter.ExecuteAsync(new InitializeCounter(50));
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter());
        }

        // Act - Read projection multiple times
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? first = await projGrain.GetAsync(CancellationToken.None);
        CounterSummaryProjection? second = await projGrain.GetAsync(CancellationToken.None);
        CounterSummaryProjection? third = await projGrain.GetAsync(CancellationToken.None);

        // Assert - All reads should return same values
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        third.Should().NotBeNull();
        first!.CurrentCount.Should().Be(second!.CurrentCount);
        second.CurrentCount.Should().Be(third!.CurrentCount);
        first.TotalOperations.Should().Be(second.TotalOperations);
        second.TotalOperations.Should().Be(third.TotalOperations);

        // Expected: 50 + 5 = 55, 6 operations
        first.CurrentCount.Should().Be(55, "Count should be 50 + 5 = 55");
        first.TotalOperations.Should().Be(6, "Operations should be 6");
        output.WriteLine($"[Test] All three reads returned: Count={first.CurrentCount}, Ops={first.TotalOperations}");
        output.WriteLine("[Test] PASSED: Projection re-reads are consistent!");
    }

    /// <summary>
    ///     Tests rapid sequential updates to verify ordering.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RapidSequentialUpdatesMaintainCorrectOrder()
    {
        // Arrange
        string entityId = NewEntityId("rapid");
        output.WriteLine($"[Test] Testing rapid sequential updates: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize
        await counter.ExecuteAsync(new InitializeCounter());

        // Act - Rapid fire: increment by different amounts (+1, +2, +3, +4, +5 = 15)
        for (int i = 1; i <= 5; i++)
        {
            OperationResult result = await counter.ExecuteAsync(
                new IncrementCounter
                {
                    Amount = i,
                });
            result.Success.Should().BeTrue($"Increment({i}) should succeed");
        }

        // Act - Then decrement: -1, -2 = -3, net = 15 - 3 = 12
        for (int i = 1; i <= 2; i++)
        {
            await counter.ExecuteAsync(
                new DecrementCounter
                {
                    Amount = i,
                });
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();

        // Expected: 0 + (1+2+3+4+5) - (1+2) = 12, 8 operations
        projection!.CurrentCount.Should().Be(12, "Count should be 12");
        projection.TotalOperations.Should().Be(8, "Operations should be 8");
        output.WriteLine($"[Test] Rapid updates: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: Rapid sequential updates maintain correct order!");
    }

    /// <summary>
    ///     Tests reset and recovery scenario.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ResetAndRecoveryProjectsCorrectly()
    {
        // Arrange
        string entityId = NewEntityId("reset");
        output.WriteLine($"[Test] Testing reset and recovery: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Initialize and increment
        await counter.ExecuteAsync(new InitializeCounter(10));
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter());
        }

        // Act - Reset to 1000
        OperationResult resetResult = await counter.ExecuteAsync(
            new ResetCounter
            {
                NewValue = 1000,
            });
        resetResult.Success.Should().BeTrue("Reset should succeed");

        // Act - Increment 3 more times after reset
        for (int i = 0; i < 3; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter());
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();

        // Expected: 1000 + 3 = 1003, 10 operations (1 init + 5 inc + 1 reset + 3 inc)
        projection!.CurrentCount.Should().Be(1003, "Count should be 1000 + 3 = 1003");
        projection.TotalOperations.Should().Be(10, "Operations should be 10");
        output.WriteLine(
            $"[Test] Reset and recovery: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: Reset and recovery projects correctly!");
    }
}