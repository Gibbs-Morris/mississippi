using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter;
using Crescent.ConsoleApp.CounterSummary;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Simple end-to-end UX projection scenario that validates the complete flow:
///     Aggregate receives command → raises events → saves to brook → projection reads and returns expected state.
/// </summary>
internal static class SimpleUxProjectionScenario
{
    private const string ScenarioName = "SimpleUxProjection";

    /// <summary>
    ///     Runs a simple end-to-end scenario with a fresh ID each time:
    ///     1. Creates a brand new counter (fresh stream)
    ///     2. Executes commands on the aggregate that raise and persist events
    ///     3. Queries the UX projection for the same ID
    ///     4. Verifies the projection state matches expected values.
    /// </summary>
    /// <param name="logger">Logger for output.</param>
    /// <param name="runId">Run correlation ID.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="uxProjectionGrainFactory">UX projection grain factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken = default
    )
    {
        Stopwatch sw = Stopwatch.StartNew();

        // Create a fresh ID for this run to ensure we're working with an empty stream
        string counterId = $"simple-ux-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        logger.SimpleUxScenarioStart(runId, counterId);

        // ==============================================================
        // Step 1: Execute commands on the aggregate (writes events)
        // ==============================================================
        logger.SimpleUxStep(runId, 1, "Execute commands on aggregate to write events to brook");
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize with value 10
        OperationResult initResult = await counter.InitializeAsync(10);
        if (!initResult.Success)
        {
            logger.SimpleUxFailed(runId, "Initialize", initResult.ErrorMessage ?? "Unknown error");
            sw.Stop();
            return ScenarioResult.Failure(
                ScenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
        }

        logger.SimpleUxCommandExecuted(runId, "Initialize(10)");

        // Increment 5 times
        for (int i = 0; i < 5; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            if (!incResult.Success)
            {
                logger.SimpleUxFailed(runId, $"Increment[{i + 1}]", incResult.ErrorMessage ?? "Unknown error");
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    incResult.ErrorMessage ?? $"Increment[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.SimpleUxCommandExecuted(runId, "Increment x5");

        // Decrement 2 times
        for (int i = 0; i < 2; i++)
        {
            OperationResult decResult = await counter.DecrementAsync();
            if (!decResult.Success)
            {
                logger.SimpleUxFailed(runId, $"Decrement[{i + 1}]", decResult.ErrorMessage ?? "Unknown error");
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    decResult.ErrorMessage ?? $"Decrement[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.SimpleUxCommandExecuted(runId, "Decrement x2");
        logger.SimpleUxStepComplete(runId, 1, "8 events written (1 init + 5 inc + 2 dec)");

        // ==============================================================
        // Step 2: Read the UX projection for the same entity ID
        // ==============================================================
        logger.SimpleUxStep(runId, 2, "Query UX projection for the same entity ID");
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.SimpleUxFailed(runId, "GetProjection", "Projection returned null");
            sw.Stop();
            return ScenarioResult.Failure(ScenarioName, "Projection returned null", (int)sw.ElapsedMilliseconds);
        }

        logger.SimpleUxProjectionReceived(runId, projection.CurrentCount, projection.TotalOperations);

        // ==============================================================
        // Step 3: Verify the projection state matches expectations
        // ==============================================================
        logger.SimpleUxStep(runId, 3, "Verify projection state matches expected values");

        // Expected: 10 (init) + 5 (increments) - 2 (decrements) = 13
        int expectedCount = 13;
        int expectedOperations = 8; // 1 init + 5 inc + 2 dec
        bool countMatches = projection.CurrentCount == expectedCount;
        bool opsMatches = projection.TotalOperations == expectedOperations;
        sw.Stop();
        if (countMatches && opsMatches)
        {
            logger.SimpleUxVerificationPassed(
                runId,
                expectedCount,
                projection.CurrentCount,
                expectedOperations,
                projection.TotalOperations);
            logger.SimpleUxScenarioComplete(runId, counterId, "PASSED");
            return ScenarioResult.Success(
                ScenarioName,
                (int)sw.ElapsedMilliseconds,
                $"Count={projection.CurrentCount}, Ops={projection.TotalOperations}");
        }

        logger.SimpleUxVerificationFailed(
            runId,
            expectedCount,
            projection.CurrentCount,
            expectedOperations,
            projection.TotalOperations);
        logger.SimpleUxScenarioComplete(runId, counterId, "FAILED");
        return ScenarioResult.Failure(
            ScenarioName,
            $"Expected Count={expectedCount}/Ops={expectedOperations}, got Count={projection.CurrentCount}/Ops={projection.TotalOperations}",
            (int)sw.ElapsedMilliseconds);
    }
}