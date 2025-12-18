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
internal static class AggregateScenarioRunner
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
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunBasicLifecycleAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.AggregateScenarioStart(runId, "BasicLifecycle", counterId);
        Stopwatch sw = Stopwatch.StartNew();

        // Build the grain key using the brook definition pattern
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync();
        LogCommandResult(logger, runId, "Initialize", initResult);
        if (!initResult.Success)
        {
            return;
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
                return;
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
                return;
            }
        }

        // Reset to 100
        OperationResult resetResult = await counter.ResetAsync(100);
        LogCommandResult(logger, runId, "Reset(100)", resetResult);
        if (!resetResult.Success)
        {
            return;
        }

        // Increment 3 more times by different amounts
        for (int i = 1; i <= 3; i++)
        {
            OperationResult incResult = await counter.IncrementAsync(i * 10);
            LogCommandResult(logger, runId, $"Increment({i * 10})", incResult);
        }

        sw.Stop();
        logger.AggregateScenarioComplete(runId, "BasicLifecycle", counterId, (int)sw.ElapsedMilliseconds);
    }

    /// <summary>
    ///     Runs a concurrency conflict scenario by attempting operations with stale expected versions.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunConcurrencyScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.AggregateScenarioStart(runId, "ConcurrencyConflict", counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync(50);
        LogCommandResult(logger, runId, "Initialize(50)", initResult);
        if (!initResult.Success)
        {
            return;
        }

        // Increment several times to build up version
        for (int i = 0; i < 5; i++)
        {
            await counter.IncrementAsync();
        }

        logger.AggregateScenarioNote(runId, "After 5 increments, version should be ~6");
        sw.Stop();
        logger.AggregateScenarioComplete(runId, "ConcurrencyConflict", counterId, (int)sw.ElapsedMilliseconds);
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
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunThroughputScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        int operationCount = 100,
        CancellationToken cancellationToken = default
    )
    {
        logger.AggregateScenarioStart(runId, $"Throughput({operationCount})", counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Initialize
        OperationResult initResult = await counter.InitializeAsync();
        if (!initResult.Success)
        {
            LogCommandResult(logger, runId, "Initialize(0)", initResult);
            return;
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
    }

    /// <summary>
    ///     Runs validation error scenarios by attempting invalid operations.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunValidationScenarioAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.AggregateScenarioStart(runId, "ValidationErrors", counterId);
        Stopwatch sw = Stopwatch.StartNew();
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);

        // Attempt increment before initialization (should fail)
        OperationResult incResult = await counter.IncrementAsync();
        LogCommandResult(logger, runId, "Increment(uninitialized) - Expected Fail", incResult);

        // Now initialize
        OperationResult initResult = await counter.InitializeAsync(10);
        LogCommandResult(logger, runId, "Initialize(10)", initResult);

        // Attempt to re-initialize (should fail)
        OperationResult reinitResult = await counter.InitializeAsync(20);
        LogCommandResult(logger, runId, "Initialize(reinit) - Expected Fail", reinitResult);

        // Attempt decrement with zero amount (should fail validation)
        OperationResult zeroDecResult = await counter.DecrementAsync(0);
        LogCommandResult(logger, runId, "Decrement(0) - Expected Fail", zeroDecResult);
        sw.Stop();
        logger.AggregateScenarioComplete(runId, "ValidationErrors", counterId, (int)sw.ElapsedMilliseconds);
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
