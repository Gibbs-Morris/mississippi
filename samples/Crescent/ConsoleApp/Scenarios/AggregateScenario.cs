using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs aggregate command scenarios and logs results.
/// </summary>
internal static class AggregateScenario
{
    /// <summary>
    ///     Runs the basic counter aggregate lifecycle scenario:
    ///     Initialize → Increment (10x) → Decrement (5x) → Reset → Increment (3x).
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunBasicLifecycleAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        const string scenarioName = "BasicLifecycle";
        logger.AggregateScenarioStart(runId, scenarioName, counterId);
        Stopwatch sw = Stopwatch.StartNew();

        // Build the grain key using the brook definition pattern
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync();
        LogCommandResult(logger, runId, "Initialize", initResult);
        if (!initResult.Success)
        {
            sw.Stop();
            return ScenarioResult.Failure(
                scenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
        }

        // Increment 10 times
        for (int i = 0; i < 10; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            if ((i == 0) || (i == 9))
            {
                LogCommandResult(logger, runId, $"Increment[{i + 1}]", incResult);
            }

            if (!incResult.Success)
            {
                sw.Stop();
                return ScenarioResult.Failure(
                    scenarioName,
                    incResult.ErrorMessage ?? $"Increment[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        // Decrement 5 times
        for (int i = 0; i < 5; i++)
        {
            OperationResult decResult = await counter.DecrementAsync();
            if ((i == 0) || (i == 4))
            {
                LogCommandResult(logger, runId, $"Decrement[{i + 1}]", decResult);
            }

            if (!decResult.Success)
            {
                sw.Stop();
                return ScenarioResult.Failure(
                    scenarioName,
                    decResult.ErrorMessage ?? $"Decrement[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        // Reset to 100
        OperationResult resetResult = await counter.ResetAsync(100);
        LogCommandResult(logger, runId, "Reset(100)", resetResult);
        if (!resetResult.Success)
        {
            sw.Stop();
            return ScenarioResult.Failure(
                scenarioName,
                resetResult.ErrorMessage ?? "Reset failed",
                (int)sw.ElapsedMilliseconds);
        }

        // Increment 3 more times by different amounts
        for (int i = 1; i <= 3; i++)
        {
            OperationResult incResult = await counter.IncrementAsync(i * 10);
            LogCommandResult(logger, runId, $"Increment({i * 10})", incResult);
            if (!incResult.Success)
            {
                sw.Stop();
                return ScenarioResult.Failure(
                    scenarioName,
                    incResult.ErrorMessage ?? $"Final Increment({i * 10}) failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        sw.Stop();
        logger.AggregateScenarioComplete(runId, scenarioName, counterId, (int)sw.ElapsedMilliseconds);
        return ScenarioResult.Success(scenarioName, (int)sw.ElapsedMilliseconds, "Lifecycle completed: 19 operations");
    }

    /// <summary>
    ///     Runs a concurrency conflict scenario by attempting operations with stale expected versions.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunConcurrencyScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        const string scenarioName = "ConcurrencyConflict";
        logger.AggregateScenarioStart(runId, scenarioName, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync(50);
        LogCommandResult(logger, runId, "Initialize(50)", initResult);
        if (!initResult.Success)
        {
            sw.Stop();
            return ScenarioResult.Failure(
                scenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
        }

        // Increment several times to build up version
        for (int i = 0; i < 5; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            if (!incResult.Success)
            {
                sw.Stop();
                return ScenarioResult.Failure(
                    scenarioName,
                    incResult.ErrorMessage ?? $"Increment[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.AggregateScenarioNote(runId, "After 5 increments, version should be ~6");
        sw.Stop();
        logger.AggregateScenarioComplete(runId, scenarioName, counterId, (int)sw.ElapsedMilliseconds);
        return ScenarioResult.Success(scenarioName, (int)sw.ElapsedMilliseconds, "Concurrency scenario completed");
    }

    /// <summary>
    ///     Runs a high-throughput scenario with many rapid operations.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="operationCount">Number of operations to perform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunThroughputScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        int operationCount = 100,
        CancellationToken cancellationToken = default
    )
    {
        string scenarioName = $"Throughput({operationCount})";
        logger.AggregateScenarioStart(runId, scenarioName, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync();
        if (!initResult.Success)
        {
            LogCommandResult(logger, runId, "Initialize(0)", initResult);
            sw.Stop();
            return ScenarioResult.Failure(
                scenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
        }

        // Run rapid increments
        int successCount = 0;
        int failCount = 0;
        for (int i = 0; i < operationCount; i++)
        {
            OperationResult result = await counter.IncrementAsync();
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        sw.Stop();
        double opsPerSecond = operationCount / Math.Max(0.001, sw.Elapsed.TotalSeconds);
        logger.AggregateThroughputResult(
            runId,
            operationCount,
            successCount,
            failCount,
            (int)sw.ElapsedMilliseconds,
            opsPerSecond);
        if (failCount > 0)
        {
            return ScenarioResult.Failure(
                scenarioName,
                $"Throughput completed with {failCount} failures",
                (int)sw.ElapsedMilliseconds);
        }

        return ScenarioResult.Success(
            scenarioName,
            (int)sw.ElapsedMilliseconds,
            $"Throughput completed: {successCount} ops at {opsPerSecond:F1} ops/sec");
    }

    /// <summary>
    ///     Runs validation error scenarios by attempting invalid operations.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunValidationScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        const string scenarioName = "ValidationErrors";
        logger.AggregateScenarioStart(runId, scenarioName, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Attempt increment before initialization (should fail)
        OperationResult incResult = await counter.IncrementAsync();
        LogCommandResult(logger, runId, "Increment(uninitialized) - Expected Fail", incResult);

        // Now initialize
        OperationResult initResult = await counter.InitializeAsync(10);
        LogCommandResult(logger, runId, "Initialize(10)", initResult);
        if (!initResult.Success)
        {
            sw.Stop();
            return ScenarioResult.Failure(scenarioName, "Initialize failed unexpectedly", (int)sw.ElapsedMilliseconds);
        }

        // Attempt to re-initialize (should fail)
        OperationResult reinitResult = await counter.InitializeAsync(20);
        LogCommandResult(logger, runId, "Initialize(reinit) - Expected Fail", reinitResult);

        // Attempt decrement with zero amount (should fail validation)
        OperationResult zeroDecResult = await counter.DecrementAsync(0);
        LogCommandResult(logger, runId, "Decrement(0) - Expected Fail", zeroDecResult);
        sw.Stop();
        logger.AggregateScenarioComplete(runId, scenarioName, counterId, (int)sw.ElapsedMilliseconds);
        return ScenarioResult.Success(scenarioName, (int)sw.ElapsedMilliseconds, "Validation errors properly detected");
    }

    private static void LogCommandResult(
        ILogger logger,
        string runId,
        string commandName,
        OperationResult result
    )
    {
        if (result.Success)
        {
            logger.AggregateCommandSuccess(runId, commandName);
        }
        else
        {
            logger.AggregateCommandFailed(runId, commandName, result.ErrorCode!, result.ErrorMessage!);
        }
    }
}