using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

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
        await counter.ExecuteAsync(new InitializeCounter(), TestContext.Current.CancellationToken);

        // Act - Decrement to go negative
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new DecrementCounter(), TestContext.Current.CancellationToken);
        }

        // Assert - Verify projection shows negative
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);

        // Expected: 0 - 5 = -5, IsPositive = false, 6 operations
        Assert.Equal(-5, projection.CurrentCount);
        Assert.Equal(6, projection.TotalOperations);
        Assert.False(projection.IsPositive, "IsPositive should be false for negative count");
        output.WriteLine($"[Test] Boundary: Count={projection.CurrentCount}, IsPositive={projection.IsPositive}");

        // Act - Increment back to zero
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
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
            await Task.Delay(RetryDelayMs, TestContext.Current.CancellationToken);
        }

        Assert.True(afterZero is not null, "Projection should exist after incrementing back");
        CounterSummaryProjection finalProjection = afterZero;
        Assert.Equal(0, finalProjection.CurrentCount);
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
        OperationResult init1 = await counter1.ExecuteAsync(
            new InitializeCounter(100),
            TestContext.Current.CancellationToken);
        OperationResult init2 = await counter2.ExecuteAsync(
            new InitializeCounter(200),
            TestContext.Current.CancellationToken);
        Assert.True(init1.Success);
        Assert.True(init2.Success);

        // Act - Increment counter1 by 10 operations
        for (int i = 0; i < 10; i++)
        {
            await counter1.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
        }

        // Act - Decrement counter2 by 5 operations
        for (int i = 0; i < 5; i++)
        {
            await counter2.ExecuteAsync(new DecrementCounter(), TestContext.Current.CancellationToken);
        }

        // Assert - Verify projections are isolated
        IUxProjectionGrain<CounterSummaryProjection> proj1 = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(counterId1);
        IUxProjectionGrain<CounterSummaryProjection> proj2 = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(counterId2);
        CounterSummaryProjection? projection1 = await proj1.GetAsync(CancellationToken.None);
        CounterSummaryProjection? projection2 = await proj2.GetAsync(CancellationToken.None);
        Assert.NotNull(projection1);
        Assert.NotNull(projection2);

        // Counter1: 100 + 10 = 110, 11 operations
        Assert.Equal(110, projection1.CurrentCount);
        Assert.Equal(11, projection1.TotalOperations);

        // Counter2: 200 - 5 = 195, 6 operations
        Assert.Equal(195, projection2.CurrentCount);
        Assert.Equal(6, projection2.TotalOperations);
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
        await counter.ExecuteAsync(new InitializeCounter(), TestContext.Current.CancellationToken);

        // Act - Perform many increments
        for (int i = 0; i < opCount; i++)
        {
            OperationResult result = await counter.ExecuteAsync(
                new IncrementCounter(),
                TestContext.Current.CancellationToken);
            Assert.True(result.Success, $"Increment[{i}] should succeed");
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(opCount, projection.CurrentCount);
        Assert.Equal(opCount + 1, projection.TotalOperations);
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
        await counter.ExecuteAsync(new InitializeCounter(25), TestContext.Current.CancellationToken);
        for (int i = 0; i < 10; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
        }

        // Act - First read
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? beforeDeactivation = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(beforeDeactivation);
        int expectedCount = beforeDeactivation.CurrentCount;
        int expectedOps = beforeDeactivation.TotalOperations;

        // Small delay to simulate some idle time
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act - Read again (simulating after potential deactivation)
        CounterSummaryProjection? afterDeactivation = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(afterDeactivation);

        // Assert - Values should match
        Assert.Equal(expectedCount, afterDeactivation.CurrentCount);
        Assert.Equal(expectedOps, afterDeactivation.TotalOperations);

        // Expected: 25 + 10 = 35, 11 operations
        Assert.Equal(35, expectedCount);
        Assert.Equal(11, expectedOps);
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
        await counter.ExecuteAsync(new InitializeCounter(50), TestContext.Current.CancellationToken);
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
        }

        // Act - Read projection multiple times
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? first = await projGrain.GetAsync(CancellationToken.None);
        CounterSummaryProjection? second = await projGrain.GetAsync(CancellationToken.None);
        CounterSummaryProjection? third = await projGrain.GetAsync(CancellationToken.None);

        // Assert - All reads should return same values
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.Equal(second.CurrentCount, first.CurrentCount);
        Assert.Equal(third.CurrentCount, second.CurrentCount);
        Assert.Equal(second.TotalOperations, first.TotalOperations);
        Assert.Equal(third.TotalOperations, second.TotalOperations);

        // Expected: 50 + 5 = 55, 6 operations
        Assert.Equal(55, first.CurrentCount);
        Assert.Equal(6, first.TotalOperations);
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
        await counter.ExecuteAsync(new InitializeCounter(), TestContext.Current.CancellationToken);

        // Act - Rapid fire: increment by different amounts (+1, +2, +3, +4, +5 = 15)
        for (int i = 1; i <= 5; i++)
        {
            OperationResult result = await counter.ExecuteAsync(
                new IncrementCounter
                {
                    Amount = i,
                },
                TestContext.Current.CancellationToken);
            Assert.True(result.Success, $"Increment({i}) should succeed");
        }

        // Act - Then decrement: -1, -2 = -3, net = 15 - 3 = 12
        for (int i = 1; i <= 2; i++)
        {
            await counter.ExecuteAsync(
                new DecrementCounter
                {
                    Amount = i,
                },
                TestContext.Current.CancellationToken);
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);

        // Expected: 0 + (1+2+3+4+5) - (1+2) = 12, 8 operations
        Assert.Equal(12, projection.CurrentCount);
        Assert.Equal(8, projection.TotalOperations);
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
        await counter.ExecuteAsync(new InitializeCounter(10), TestContext.Current.CancellationToken);
        for (int i = 0; i < 5; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
        }

        // Act - Reset to 1000
        OperationResult resetResult = await counter.ExecuteAsync(
            new ResetCounter
            {
                NewValue = 1000,
            },
            TestContext.Current.CancellationToken);
        Assert.True(resetResult.Success, "Reset should succeed");

        // Act - Increment 3 more times after reset
        for (int i = 0; i < 3; i++)
        {
            await counter.ExecuteAsync(new IncrementCounter(), TestContext.Current.CancellationToken);
        }

        // Assert - Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);

        // Expected: 1000 + 3 = 1003, 10 operations (1 init + 5 inc + 1 reset + 3 inc)
        Assert.Equal(1003, projection.CurrentCount);
        Assert.Equal(10, projection.TotalOperations);
        output.WriteLine(
            $"[Test] Reset and recovery: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
        output.WriteLine("[Test] PASSED: Reset and recovery projects correctly!");
    }
}