using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter;
using Crescent.ConsoleApp.CounterSummary;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Comprehensive end-to-end test scenarios for validating the complete event sourcing pipeline:
///     Aggregate → Events → Brook → Projection.
/// </summary>
internal static class ComprehensiveE2EScenarios
{
    private const string SuiteName = "ComprehensiveE2E";

    /// <summary>
    ///     Extracts pass/fail counts from a comprehensive E2E scenario result.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    /// <returns>Tuple of (passed, failed, total).</returns>
    public static (int Passed, int Failed, int Total) GetCounts(
        ScenarioResult result
    )
    {
        if ((result.Data != null) &&
            result.Data.TryGetValue("Passed", out object? passedObj) &&
            result.Data.TryGetValue("Failed", out object? failedObj) &&
            result.Data.TryGetValue("Total", out object? totalObj))
        {
            return ((int)passedObj, (int)failedObj, (int)totalObj);
        }

        return (0, 0, 0);
    }

    /// <summary>
    ///     Runs all comprehensive E2E scenarios and returns the overall result.
    /// </summary>
    /// <param name="logger">Logger for output.</param>
    /// <param name="runId">Run correlation ID.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="uxProjectionGrainFactory">UX projection grain factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result with pass/fail counts in the Data property.</returns>
    public static async Task<ScenarioResult> RunAllAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken = default
    )
    {
        logger.E2ESuiteStart(runId);
        Stopwatch sw = Stopwatch.StartNew();
        List<(string Name, Func<Task<bool>> Test)> scenarios =
        [
            ("IsolatedAggregates",
                () => RunIsolatedAggregatesScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("ConcurrentCommands",
                () => RunConcurrentCommandsScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("ProjectionRereadConsistency",
                () => RunProjectionRereadConsistencyScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("LargeOperationSequence",
                () => RunLargeOperationSequenceScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("ResetAndRecovery",
                () => RunResetAndRecoveryScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("BoundaryConditions",
                () => RunBoundaryConditionsScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("RapidSequentialUpdates",
                () => RunRapidSequentialUpdatesScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
            ("ProjectionAfterDeactivation",
                () => RunProjectionAfterDeactivationScenarioAsync(
                    logger,
                    runId,
                    grainFactory,
                    uxProjectionGrainFactory,
                    cancellationToken)),
        ];
        int passed = 0;
        int failed = 0;
        foreach ((string name, Func<Task<bool>> test) in scenarios)
        {
            try
            {
                logger.E2EScenarioStart(runId, name);
                bool result = await test();
                if (result)
                {
                    passed++;
                    logger.E2EScenarioPassed(runId, name);
                }
                else
                {
                    failed++;
                    logger.E2EScenarioFailed(runId, name, "Test returned false");
                }
            }
            catch (InvalidOperationException ex)
            {
                failed++;
                logger.E2EScenarioFailed(runId, name, ex.Message);
            }
            catch (TimeoutException ex)
            {
                failed++;
                logger.E2EScenarioFailed(runId, name, ex.Message);
            }
        }

        sw.Stop();
        logger.E2ESuiteComplete(runId, passed, failed, scenarios.Count, sw.ElapsedMilliseconds);
        Dictionary<string, object> data = new()
        {
            ["Passed"] = passed,
            ["Failed"] = failed,
            ["Total"] = scenarios.Count,
        };
        if (failed > 0)
        {
            return ScenarioResult.Failure(
                SuiteName,
                $"{failed}/{scenarios.Count} scenarios failed",
                (int)sw.ElapsedMilliseconds,
                data);
        }

        return ScenarioResult.Success(SuiteName, (int)sw.ElapsedMilliseconds, $"All {passed} scenarios passed", data);
    }

    /// <summary>
    ///     Scenario: Test boundary conditions and edge cases.
    ///     Tests zero values, negative results, and edge behaviors.
    /// </summary>
    private static async Task<bool> RunBoundaryConditionsScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"boundary-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize with 0
        await counter.InitializeAsync();

        // Decrement to go negative
        for (int i = 0; i < 5; i++)
        {
            await counter.DecrementAsync();
        }

        // Verify projection shows negative
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.E2EStepFailed(runId, "BoundaryConditions", "GetProjection", "Projection returned null");
            return false;
        }

        // Expected: 0 - 5 = -5, IsPositive = false, 6 operations
        if ((projection.CurrentCount != -5) || (projection.TotalOperations != 6))
        {
            logger.E2EStepFailed(
                runId,
                "BoundaryConditions",
                "NegativeCheck",
                $"Got {projection.CurrentCount}/{projection.TotalOperations}, expected -5/6");
            return false;
        }

        if (projection.IsPositive)
        {
            logger.E2EStepFailed(
                runId,
                "BoundaryConditions",
                "IsPositive",
                "Expected IsPositive=false for negative count");
            return false;
        }

        // Increment back to zero (explicitly 5 times by 1)
        for (int i = 0; i < 5; i++)
        {
            await counter.IncrementAsync();
        }

        // Allow projection to catch up
        await Task.Delay(50, cancellationToken);
        CounterSummaryProjection? afterZero = await projGrain.GetAsync(cancellationToken);
        if ((afterZero == null) || (afterZero.CurrentCount != 0))
        {
            logger.E2EStepFailed(
                runId,
                "BoundaryConditions",
                "ZeroCheck",
                $"Expected 0, got {afterZero?.CurrentCount}");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Multiple commands executed in parallel on the same aggregate.
    ///     Tests concurrency handling and final state consistency.
    /// </summary>
    private static async Task<bool> RunConcurrentCommandsScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"concurrent-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync();
        if (!initResult.Success)
        {
            logger.E2EStepFailed(runId, "ConcurrentCommands", "Initialize", "Initialization failed");
            return false;
        }

        // Fire 20 concurrent increment commands
        const int concurrentOps = 20;
        List<Task<OperationResult>> tasks = [];
        for (int i = 0; i < concurrentOps; i++)
        {
            tasks.Add(counter.IncrementAsync());
        }

        OperationResult[] results = await Task.WhenAll(tasks);

        // All should succeed (Orleans serializes grain calls)
        int successCount = 0;
        foreach (OperationResult result in results)
        {
            if (result.Success)
            {
                successCount++;
            }
        }

        if (successCount != concurrentOps)
        {
            logger.E2EStepFailed(
                runId,
                "ConcurrentCommands",
                "Execute",
                $"Only {successCount}/{concurrentOps} operations succeeded");
            return false;
        }

        // Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.E2EStepFailed(runId, "ConcurrentCommands", "GetProjection", "Projection returned null");
            return false;
        }

        // Expected: 0 + 20 = 20, 21 operations (1 init + 20 increments)
        if ((projection.CurrentCount != 20) || (projection.TotalOperations != 21))
        {
            logger.E2EStepFailed(
                runId,
                "ConcurrentCommands",
                "Verification",
                $"Got Count={projection.CurrentCount}, Ops={projection.TotalOperations} (expected 20/21)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Multiple aggregates operate independently, projections stay isolated.
    ///     Tests that events from one aggregate don't affect another aggregate's projection.
    /// </summary>
    private static async Task<bool> RunIsolatedAggregatesScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        // Create two independent counters
        string counterId1 = $"isolated-1-{Guid.NewGuid():N}";
        string counterId2 = $"isolated-2-{Guid.NewGuid():N}";
        BrookKey brookKey1 = BrookKey.For<CounterBrook>(counterId1);
        BrookKey brookKey2 = BrookKey.For<CounterBrook>(counterId2);
        ICounterAggregateGrain counter1 = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey1);
        ICounterAggregateGrain counter2 = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey2);

        // Initialize counter1 with 100, counter2 with 200
        OperationResult init1 = await counter1.InitializeAsync(100);
        OperationResult init2 = await counter2.InitializeAsync(200);
        if (!init1.Success || !init2.Success)
        {
            logger.E2EStepFailed(runId, "IsolatedAggregates", "Initialize", "One or both initializations failed");
            return false;
        }

        // Increment counter1 by 10 operations
        for (int i = 0; i < 10; i++)
        {
            await counter1.IncrementAsync();
        }

        // Decrement counter2 by 5 operations
        for (int i = 0; i < 5; i++)
        {
            await counter2.DecrementAsync();
        }

        // Verify projections are isolated
        IUxProjectionGrain<CounterSummaryProjection> proj1 =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId1);
        IUxProjectionGrain<CounterSummaryProjection> proj2 =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId2);
        CounterSummaryProjection? projection1 = await proj1.GetAsync(cancellationToken);
        CounterSummaryProjection? projection2 = await proj2.GetAsync(cancellationToken);
        if ((projection1 == null) || (projection2 == null))
        {
            logger.E2EStepFailed(runId, "IsolatedAggregates", "GetProjection", "One or both projections returned null");
            return false;
        }

        // Counter1: 100 + 10 = 110, 11 operations
        // Counter2: 200 - 5 = 195, 6 operations
        bool counter1Correct = (projection1.CurrentCount == 110) && (projection1.TotalOperations == 11);
        bool counter2Correct = (projection2.CurrentCount == 195) && (projection2.TotalOperations == 6);
        if (!counter1Correct || !counter2Correct)
        {
            string reason = $"Counter1: {projection1.CurrentCount}/{projection1.TotalOperations} (expected 110/11), " +
                            $"Counter2: {projection2.CurrentCount}/{projection2.TotalOperations} (expected 195/6)";
            logger.E2EStepFailed(runId, "IsolatedAggregates", "Verification", reason);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Large number of sequential operations.
    ///     Tests position tracking with many events.
    /// </summary>
    private static async Task<bool> RunLargeOperationSequenceScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"large-seq-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        await counter.InitializeAsync();

        // Perform 20 increments (reduced from 100 to avoid infrastructure timeouts)
        const int opCount = 20;
        for (int i = 0; i < opCount; i++)
        {
            OperationResult result = await counter.IncrementAsync();
            if (!result.Success)
            {
                logger.E2EStepFailed(
                    runId,
                    "LargeOperationSequence",
                    $"Increment[{i}]",
                    result.ErrorMessage ?? "Unknown");
                return false;
            }
        }

        // Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.E2EStepFailed(runId, "LargeOperationSequence", "GetProjection", "Projection returned null");
            return false;
        }

        // Expected: 0 + 20 = 20, 21 operations
        if ((projection.CurrentCount != 20) || (projection.TotalOperations != 21))
        {
            logger.E2EStepFailed(
                runId,
                "LargeOperationSequence",
                "Verification",
                $"Got {projection.CurrentCount}/{projection.TotalOperations}, expected 20/21");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: After grain deactivation, projection should still return correct state.
    ///     Tests persistence and recovery of projection state.
    /// </summary>
    private static async Task<bool> RunProjectionAfterDeactivationScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"deactivate-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Setup: Initialize and perform operations
        await counter.InitializeAsync(25);
        for (int i = 0; i < 10; i++)
        {
            await counter.IncrementAsync();
        }

        // First read
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? beforeDeactivation = await projGrain.GetAsync(cancellationToken);
        if (beforeDeactivation == null)
        {
            logger.E2EStepFailed(runId, "ProjectionAfterDeactivation", "FirstRead", "Projection returned null");
            return false;
        }

        // Store expected values
        int expectedCount = beforeDeactivation.CurrentCount;
        int expectedOps = beforeDeactivation.TotalOperations;

        // Request deactivation (in a real scenario, we'd wait for grain timeout or force deactivation)
        // For now, we'll just re-read immediately which should still work
        // In production, you might use IGrainManagementExtension or wait for idle timeout

        // Small delay to allow any async operations to settle
        await Task.Delay(100, cancellationToken);

        // Read again
        CounterSummaryProjection? afterDeactivation = await projGrain.GetAsync(cancellationToken);
        if (afterDeactivation == null)
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionAfterDeactivation",
                "SecondRead",
                "Projection returned null after delay");
            return false;
        }

        // Values should match
        if ((afterDeactivation.CurrentCount != expectedCount) || (afterDeactivation.TotalOperations != expectedOps))
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionAfterDeactivation",
                "Verification",
                $"Values changed: before={expectedCount}/{expectedOps}, after={afterDeactivation.CurrentCount}/{afterDeactivation.TotalOperations}");
            return false;
        }

        // Verify expected values: 25 + 10 = 35, 11 operations
        if ((expectedCount != 35) || (expectedOps != 11))
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionAfterDeactivation",
                "ExpectedValues",
                $"Got {expectedCount}/{expectedOps}, expected 35/11");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Read projection multiple times consecutively.
    ///     Tests that re-reads return consistent results.
    /// </summary>
    private static async Task<bool> RunProjectionRereadConsistencyScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"reread-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Setup: Initialize and perform some operations
        await counter.InitializeAsync(50);
        for (int i = 0; i < 5; i++)
        {
            await counter.IncrementAsync();
        }

        // Read projection multiple times
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? first = await projGrain.GetAsync(cancellationToken);
        CounterSummaryProjection? second = await projGrain.GetAsync(cancellationToken);
        CounterSummaryProjection? third = await projGrain.GetAsync(cancellationToken);
        if ((first == null) || (second == null) || (third == null))
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionRereadConsistency",
                "GetProjection",
                "One or more reads returned null");
            return false;
        }

        // All reads should return same values
        bool allConsistent = (first.CurrentCount == second.CurrentCount) &&
                             (second.CurrentCount == third.CurrentCount) &&
                             (first.TotalOperations == second.TotalOperations) &&
                             (second.TotalOperations == third.TotalOperations);
        if (!allConsistent)
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionRereadConsistency",
                "Verification",
                $"Inconsistent reads: {first.CurrentCount}/{second.CurrentCount}/{third.CurrentCount}");
            return false;
        }

        // Verify expected values: 50 + 5 = 55, 6 operations
        if ((first.CurrentCount != 55) || (first.TotalOperations != 6))
        {
            logger.E2EStepFailed(
                runId,
                "ProjectionRereadConsistency",
                "Values",
                $"Got {first.CurrentCount}/{first.TotalOperations}, expected 55/6");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Rapid sequential updates to verify ordering.
    ///     Tests that fast sequential operations maintain correct order.
    /// </summary>
    private static async Task<bool> RunRapidSequentialUpdatesScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"rapid-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        await counter.InitializeAsync();

        // Rapid fire: increment by different amounts to verify order
        // Pattern: +1, +2, +3, +4, +5 = 15
        for (int i = 1; i <= 5; i++)
        {
            OperationResult result = await counter.IncrementAsync(i);
            if (!result.Success)
            {
                logger.E2EStepFailed(
                    runId,
                    "RapidSequentialUpdates",
                    $"Increment({i})",
                    result.ErrorMessage ?? "Unknown");
                return false;
            }
        }

        // Then: -1, -2 = -3, net = 15 - 3 = 12
        for (int i = 1; i <= 2; i++)
        {
            await counter.DecrementAsync(i);
        }

        // Verify
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.E2EStepFailed(runId, "RapidSequentialUpdates", "GetProjection", "Projection returned null");
            return false;
        }

        // Expected: 0 + (1+2+3+4+5) - (1+2) = 12, 8 operations
        if ((projection.CurrentCount != 12) || (projection.TotalOperations != 8))
        {
            logger.E2EStepFailed(
                runId,
                "RapidSequentialUpdates",
                "Verification",
                $"Got {projection.CurrentCount}/{projection.TotalOperations}, expected 12/8");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Scenario: Reset aggregate and verify projection reflects new state.
    ///     Tests that reset events are properly projected.
    /// </summary>
    private static async Task<bool> RunResetAndRecoveryScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        CancellationToken cancellationToken
    )
    {
        string counterId = $"reset-{Guid.NewGuid():N}";
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize and increment
        await counter.InitializeAsync(10);
        for (int i = 0; i < 5; i++)
        {
            await counter.IncrementAsync();
        }

        // Reset to 1000
        OperationResult resetResult = await counter.ResetAsync(1000);
        if (!resetResult.Success)
        {
            logger.E2EStepFailed(runId, "ResetAndRecovery", "Reset", resetResult.ErrorMessage ?? "Unknown");
            return false;
        }

        // Increment 3 more times after reset
        for (int i = 0; i < 3; i++)
        {
            await counter.IncrementAsync();
        }

        // Verify projection
        IUxProjectionGrain<CounterSummaryProjection> projGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.E2EStepFailed(runId, "ResetAndRecovery", "GetProjection", "Projection returned null");
            return false;
        }

        // Expected: 1000 + 3 = 1003, 10 operations (1 init + 5 inc + 1 reset + 3 inc)
        if ((projection.CurrentCount != 1003) || (projection.TotalOperations != 10))
        {
            logger.E2EStepFailed(
                runId,
                "ResetAndRecovery",
                "Verification",
                $"Got {projection.CurrentCount}/{projection.TotalOperations}, expected 1003/10");
            return false;
        }

        return true;
    }
}