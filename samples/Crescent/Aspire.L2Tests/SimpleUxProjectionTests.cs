using Crescent.Aspire.L2Tests.Domain.Counter;
using Crescent.Aspire.L2Tests.Domain.CounterSummary;

using FluentAssertions;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Xunit;
using Xunit.Abstractions;

namespace Crescent.Aspire.L2Tests;

/// <summary>
///     Tests validating the complete aggregate → events → projection flow.
///     Migrated from ConsoleApp SimpleUxProjectionScenario.
/// </summary>
[Collection(AspireTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class SimpleUxProjectionTests
#pragma warning restore CA1515
{
    private readonly AspireFixture fixture;
    private readonly ITestOutputHelper output;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SimpleUxProjectionTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    /// <param name="output">The xUnit test output helper.</param>
    public SimpleUxProjectionTests(
        AspireFixture fixture,
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

        ICounterAggregateGrain counter = fixture.AggregateGrainFactory
            .GetAggregate<ICounterAggregateGrain>(entityId);

        // Act - Step 1: Execute commands on aggregate (writes events)
        output.WriteLine("[Test] Step 1: Execute commands on aggregate to write events to brook");

        OperationResult initResult = await counter.InitializeAsync(10);
        initResult.Success.Should().BeTrue("Initialize should succeed");
        output.WriteLine("[Test] Command executed: Initialize(10)");

        for (int i = 0; i < 5; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            incResult.Success.Should().BeTrue($"Increment[{i + 1}] should succeed");
        }

        output.WriteLine("[Test] Command executed: Increment x5");

        for (int i = 0; i < 2; i++)
        {
            OperationResult decResult = await counter.DecrementAsync();
            decResult.Success.Should().BeTrue($"Decrement[{i + 1}] should succeed");
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

        projection.Should().NotBeNull("Projection should exist after commands");

        // Expected: 10 (init) + 5 (increments) - 2 (decrements) = 13
        int expectedCount = 13;
        int expectedOperations = 8; // 1 init + 5 inc + 2 dec

        output.WriteLine($"[Test] Expected: Count={expectedCount}, Operations={expectedOperations}");
        output.WriteLine($"[Test] Actual: Count={projection!.CurrentCount}, Operations={projection.TotalOperations}");

        projection.CurrentCount.Should().Be(expectedCount, "Count should be 10 + 5 - 2 = 13");
        projection.TotalOperations.Should().Be(expectedOperations, "Operations should be 1 + 5 + 2 = 8");
        projection.IsPositive.Should().BeTrue("Count is positive");
        projection.DisplayLabel.Should().Be($"Counter: {expectedCount}");

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
            output.WriteLine($"[Test] Projection returned default values: Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
            projection.CurrentCount.Should().Be(0, "Non-existent entity should have zero count");
            projection.TotalOperations.Should().Be(0, "Non-existent entity should have zero operations");
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

        ICounterAggregateGrain counter = fixture.AggregateGrainFactory
            .GetAggregate<ICounterAggregateGrain>(entityId);

        // Act - First initialization should succeed
        OperationResult firstInit = await counter.InitializeAsync(100);
        firstInit.Success.Should().BeTrue("First initialization should succeed");
        output.WriteLine("[Test] First Initialize(100) succeeded");

        // Act - Second initialization should fail
        OperationResult secondInit = await counter.InitializeAsync(200);
        secondInit.Success.Should().BeFalse("Second initialization should fail");
        secondInit.ErrorCode.Should().Be(AggregateErrorCodes.AlreadyExists);
        output.WriteLine($"[Test] Second Initialize(200) failed as expected: {secondInit.ErrorMessage}");

        // Assert - Projection should reflect first initialization only
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain = fixture.UxProjectionGrainFactory
            .GetUxProjectionGrain<CounterSummaryProjection>(entityId);

        CounterSummaryProjection? projection = await projectionGrain.GetAsync(CancellationToken.None);
        projection.Should().NotBeNull();
        projection!.CurrentCount.Should().Be(100, "Should reflect first init value");
        projection.TotalOperations.Should().Be(1, "Only one successful operation");

        output.WriteLine("[Test] PASSED: Re-initialization correctly prevented");
    }
}
