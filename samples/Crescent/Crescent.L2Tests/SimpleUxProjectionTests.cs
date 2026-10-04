using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Tests validating the complete aggregate → events → projection flow.
///     Migrated from ConsoleApp SimpleUxProjectionScenario.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class SimpleUxProjectionTests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    private readonly ITestOutputHelper output;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SimpleUxProjectionTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    /// <param name="output">The xUnit test output helper.</param>
    public SimpleUxProjectionTests(
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
    private static string NewEntityId() => $"simple-ux-{Guid.NewGuid():N}";

    /// <summary>
    ///     Validates that commands on an aggregate correctly project to a UX projection.
    ///     This is the core end-to-end test: aggregate receives commands → raises events → projection reads state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CommandsOnAggregateProjectCorrectState()
    {
        // Arrange - fresh ID for isolation
        string entityId = NewEntityId();
        output.WriteLine($"[Test] Starting with entity ID: {entityId}");

        // Use the generic aggregate grain pattern - brook name derived from [BrookName] on CounterAggregate
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - Step 1: Execute commands on aggregate (writes events)
        output.WriteLine("[Test] Step 1: Execute commands on aggregate to write events to brook");
        OperationResult initResult = await counter.ExecuteAsync(
            new InitializeCounter(10),
            TestContext.Current.CancellationToken);
        Assert.True(initResult.Success, "Initialize should succeed");
        output.WriteLine("[Test] Command executed: Initialize(10)");
        for (int i = 0; i < 5; i++)
        {
            OperationResult incResult = await counter.ExecuteAsync(
                new IncrementCounter(),
                TestContext.Current.CancellationToken);
            Assert.True(incResult.Success, $"Increment[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Command executed: Increment x5");
        for (int i = 0; i < 2; i++)
        {
            OperationResult decResult = await counter.ExecuteAsync(
                new DecrementCounter(),
                TestContext.Current.CancellationToken);
            Assert.True(decResult.Success, $"Decrement[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Command executed: Decrement x2");
        output.WriteLine("[Test] Step 1 complete: 8 events written (1 init + 5 inc + 2 dec)");

        // Act - Step 2: Read the UX projection for the same entity ID
        output.WriteLine("[Test] Step 2: Query UX projection for the same entity ID");
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);

        // Assert - Step 3: Verify the projection state matches expectations
        output.WriteLine("[Test] Step 3: Verify projection state matches expected values");
        Assert.True(projection is not null, "Projection should exist after commands");

        // Expected: 10 (init) + 5 (increments) - 2 (decrements) = 13
        int expectedCount = 13;
        int expectedOperations = 8; // 1 init + 5 inc + 2 dec
        output.WriteLine($"[Test] Expected: Count={expectedCount}, Operations={expectedOperations}");
        output.WriteLine($"[Test] Actual: Count={projection.CurrentCount}, Operations={projection.TotalOperations}");
        Assert.Equal(expectedCount, projection.CurrentCount);
        Assert.Equal(expectedOperations, projection.TotalOperations);
        Assert.True(projection.IsPositive, "Count is positive");
        Assert.Equal($"Counter: {expectedCount}", projection.DisplayLabel);
        output.WriteLine("[Test] PASSED: Aggregate → Events → Projection flow verified!");
    }

    /// <summary>
    ///     Validates that reading a projection for a non-existent entity returns null (or default state).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task NonExistentEntityReturnsNullProjection()
    {
        // Arrange - ID that was never initialized
        string entityId = NewEntityId();
        output.WriteLine($"[Test] Querying non-existent counter: {entityId}");

        // Act
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);

        // Assert - null or default (no events = no projection)
        // Note: The actual behavior depends on Mississippi implementation
        // Either null or a default projection with zero values is acceptable
        if (projection is null)
        {
            output.WriteLine("[Test] Projection is null for non-existent entity (expected)");
        }
        else
        {
            output.WriteLine(
                $"[Test] Projection returned default values: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
            Assert.Equal(0, projection.CurrentCount);
            Assert.Equal(0, projection.TotalOperations);
        }

        output.WriteLine("[Test] PASSED: Non-existent entity handled correctly");
    }

    /// <summary>
    ///     Validates that re-initializing an already-initialized counter fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ReinitializingCounterFails()
    {
        // Arrange
        string entityId = NewEntityId();
        output.WriteLine($"[Test] Testing re-initialization prevention: {entityId}");
        IGenericAggregateGrain<CounterAggregate> counter = fixture.AggregateGrainFactory
            .GetGenericAggregate<CounterAggregate>(entityId);

        // Act - First initialization should succeed
        OperationResult firstInit = await counter.ExecuteAsync(
            new InitializeCounter(100),
            TestContext.Current.CancellationToken);
        Assert.True(firstInit.Success, "First initialization should succeed");
        output.WriteLine("[Test] First Initialize(100) succeeded");

        // Act - Second initialization should fail
        OperationResult secondInit = await counter.ExecuteAsync(
            new InitializeCounter(200),
            TestContext.Current.CancellationToken);
        Assert.False(secondInit.Success, "Second initialization should fail");
        Assert.Equal(AggregateErrorCodes.AlreadyExists, secondInit.ErrorCode);
        output.WriteLine($"[Test] Second Initialize(200) failed as expected: {secondInit.ErrorMessage}");

        // Assert - Projection should reflect first initialization only
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        Assert.NotNull(projection);
        Assert.Equal(100, projection.CurrentCount);
        Assert.Equal(1, projection.TotalOperations);
        output.WriteLine("[Test] PASSED: Re-initialization correctly prevented");
    }
}