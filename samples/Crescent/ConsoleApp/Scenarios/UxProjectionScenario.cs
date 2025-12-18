using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter;
using Crescent.ConsoleApp.CounterSummary;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs end-to-end UX projection scenarios that validate projection snapshot persistence.
/// </summary>
internal static class UxProjectionScenario
{
    private const string ScenarioName = "UxProjectionEndToEnd";

    /// <summary>
    ///     Runs an end-to-end UX projection scenario that:
    ///     1. Creates an aggregate and writes events via commands
    ///     2. Queries the UX projection grain to get the cached projection
    ///     3. Reads projection snapshot data directly from Cosmos to verify persistence.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory for resolving grains.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="snapshotStorageProvider">Direct storage provider for reading snapshots.</param>
    /// <param name="snapshotStateConverter">Converter for deserializing projection snapshot state.</param>
    /// <param name="rootReducer">Root reducer for getting the reducer hash.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunEndToEndUxProjectionAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ISnapshotStorageProvider snapshotStorageProvider,
        ISnapshotStateConverter<CounterSummaryProjection> snapshotStateConverter,
        IRootReducer<CounterSummaryProjection> rootReducer,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.UxProjectionScenarioStart(runId, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        bool allPassed = true;

        // Step 1: Build the grain key and perform aggregate operations
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);
        logger.UxProjectionStep(runId, 1, "Execute aggregate commands to generate events for projection");

        // Initialize with value 25
        OperationResult initResult = await counter.InitializeAsync(25);
        if (!initResult.Success)
        {
            logger.UxProjectionStepFailed(runId, 1, "Initialize failed", initResult.ErrorMessage ?? "Unknown");
            sw.Stop();
            return ScenarioResult.Failure(
                ScenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
        }

        logger.UxProjectionCommandExecuted(runId, "Initialize(25)");

        // Increment 10 times
        for (int i = 0; i < 10; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            if (!incResult.Success)
            {
                logger.UxProjectionStepFailed(
                    runId,
                    1,
                    $"Increment[{i + 1}] failed",
                    incResult.ErrorMessage ?? "Unknown");
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    incResult.ErrorMessage ?? $"Increment[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.UxProjectionCommandExecuted(runId, "Increment x10");

        // Decrement 3 times
        for (int i = 0; i < 3; i++)
        {
            OperationResult decResult = await counter.DecrementAsync();
            if (!decResult.Success)
            {
                logger.UxProjectionStepFailed(
                    runId,
                    1,
                    $"Decrement[{i + 1}] failed",
                    decResult.ErrorMessage ?? "Unknown");
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    decResult.ErrorMessage ?? $"Decrement[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.UxProjectionCommandExecuted(runId, "Decrement x3");
        logger.UxProjectionStepComplete(runId, 1, "14 commands executed (1 init + 10 inc + 3 dec)");

        // Step 2: Query the UX projection grain
        logger.UxProjectionStep(runId, 2, "Query UX projection grain for cached projection state");
        IUxProjectionGrain<CounterSummaryProjection> projectionGrain =
            uxProjectionGrainFactory.GetUxProjectionGrain<CounterSummaryProjection, CounterBrook>(counterId);
        CounterSummaryProjection? projection = await projectionGrain.GetAsync(cancellationToken);
        if (projection == null)
        {
            logger.UxProjectionNotFound(runId, "CounterSummaryProjection", counterId);
            logger.UxProjectionStepFailed(runId, 2, "Projection not null", "Got null projection");
            allPassed = false;
        }
        else
        {
            logger.UxProjectionFound(runId, "CounterSummaryProjection", counterId, projection.CurrentCount);

            // Expected final value: 25 (init) + 10 (increments) - 3 (decrements) = 32
            int expectedValue = 32;
            int expectedOperations = 14;
            if ((projection.CurrentCount == expectedValue) && (projection.TotalOperations == expectedOperations))
            {
                logger.UxProjectionStepComplete(
                    runId,
                    2,
                    $"Projection state Count={projection.CurrentCount} Operations={projection.TotalOperations} matches expected");
                logger.UxProjectionDetails(
                    runId,
                    projection.CurrentCount,
                    projection.TotalOperations,
                    projection.DisplayLabel,
                    projection.IsPositive);
            }
            else
            {
                logger.UxProjectionStepFailed(
                    runId,
                    2,
                    $"Expected Count={expectedValue}, Operations={expectedOperations}",
                    $"Got Count={projection.CurrentCount}, Operations={projection.TotalOperations}");
                allPassed = false;
            }
        }

        // Step 3: Read projection snapshot directly from Cosmos to verify persistence
        logger.UxProjectionStep(runId, 3, "Read projection snapshot directly from Cosmos");

        // Build the snapshot key using the projection's snapshot name and reducer hash
        string reducerHash = rootReducer.GetReducerHash();
        string projectionType = "CRESCENT.SAMPLE.COUNTERSUMMARY.V1"; // From [SnapshotName] attribute
        SnapshotStreamKey streamKey = new(projectionType, counterId, reducerHash);

        // We need to find the snapshot version - read the latest available
        // First, let's try to read at version that matches expected event count (14)
        SnapshotKey snapshotKey = new(streamKey, 14);
        SnapshotEnvelope? envelope = await snapshotStorageProvider.ReadAsync(snapshotKey, cancellationToken);
        if (envelope == null)
        {
            // Try reading without specific version - the snapshot might be at a different position
            logger.UxProjectionSnapshotNotFound(runId, projectionType, counterId, 14);
            logger.UxProjectionStepFailed(runId, 3, "Snapshot found at version 14", "ReadAsync returned null");
            allPassed = false;
        }
        else
        {
            logger.UxProjectionSnapshotFound(
                runId,
                projectionType,
                counterId,
                snapshotKey.Version,
                envelope.Data.Length);

            // Deserialize the snapshot data using the state converter
            CounterSummaryProjection snapshotState = snapshotStateConverter.FromEnvelope(envelope);
            logger.UxProjectionSnapshotDetails(
                runId,
                envelope.DataContentType,
                envelope.ReducerHash,
                $"Count={snapshotState.CurrentCount}, Ops={snapshotState.TotalOperations}");

            // Verify snapshot matches expected values
            int expectedValue = 32;
            int expectedOperations = 14;
            if ((snapshotState.CurrentCount == expectedValue) && (snapshotState.TotalOperations == expectedOperations))
            {
                logger.UxProjectionStepComplete(
                    runId,
                    3,
                    $"Snapshot state Count={snapshotState.CurrentCount}, Operations={snapshotState.TotalOperations} matches expected");
            }
            else
            {
                logger.UxProjectionStepFailed(
                    runId,
                    3,
                    $"Expected Count={expectedValue}, Operations={expectedOperations}",
                    $"Got Count={snapshotState.CurrentCount}, Operations={snapshotState.TotalOperations}");
                allPassed = false;
            }
        }

        sw.Stop();
        if (allPassed)
        {
            logger.UxProjectionScenarioComplete(runId, counterId, (int)sw.ElapsedMilliseconds, "PASSED");
            return ScenarioResult.Success(ScenarioName, (int)sw.ElapsedMilliseconds, "All verification steps passed");
        }

        logger.UxProjectionScenarioComplete(runId, counterId, (int)sw.ElapsedMilliseconds, "FAILED");
        return ScenarioResult.Failure(
            ScenarioName,
            "One or more verification steps failed",
            (int)sw.ElapsedMilliseconds);
    }
}