using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Domain;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp;

/// <summary>
///     Runs end-to-end verification scenarios that validate event stream persistence.
/// </summary>
internal static class VerificationScenarioRunner
{
    /// <summary>
    ///     Runs an end-to-end verification scenario that:
    ///     1. Creates an aggregate and writes events via commands
    ///     2. Reads stream data directly from Cosmos to verify events were persisted
    ///     3. Reads snapshot data directly from Cosmos to verify snapshot was saved.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="grainFactory">Orleans grain factory.</param>
    /// <param name="brookStorageProvider">Direct storage provider for reading events.</param>
    /// <param name="snapshotStorageProvider">Direct storage provider for reading snapshots.</param>
    /// <param name="snapshotStateConverter">Converter for deserializing snapshot state.</param>
    /// <param name="rootReducer">Root reducer for getting the reducer hash.</param>
    /// <param name="counterId">The counter entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if verification passed, false otherwise.</returns>
    public static async Task<bool> RunEndToEndVerificationAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IBrookStorageProvider brookStorageProvider,
        ISnapshotStorageProvider snapshotStorageProvider,
        ISnapshotStateConverter<CounterState> snapshotStateConverter,
        IRootReducer<CounterState> rootReducer,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.VerificationScenarioStart(runId, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        bool allPassed = true;

        // Step 1: Build the grain key and perform aggregate operations
        BrookKey brookKey = BrookKey.For<CounterBrook>(counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);
        logger.VerificationStep(runId, 1, "Execute aggregate commands to generate events");

        // Initialize with value 10
        OperationResult initResult = await counter.InitializeAsync(10);
        if (!initResult.Success)
        {
            logger.VerificationStepFailed(runId, 1, "Initialize failed", initResult.ErrorMessage ?? "Unknown");
            return false;
        }

        logger.VerificationCommandExecuted(runId, "Initialize(10)");

        // Increment 5 times
        for (int i = 0; i < 5; i++)
        {
            OperationResult incResult = await counter.IncrementAsync();
            if (!incResult.Success)
            {
                logger.VerificationStepFailed(
                    runId,
                    1,
                    $"Increment[{i + 1}] failed",
                    incResult.ErrorMessage ?? "Unknown");
                return false;
            }
        }

        logger.VerificationCommandExecuted(runId, "Increment x5");

        // Decrement 2 times
        for (int i = 0; i < 2; i++)
        {
            OperationResult decResult = await counter.DecrementAsync();
            if (!decResult.Success)
            {
                logger.VerificationStepFailed(
                    runId,
                    1,
                    $"Decrement[{i + 1}] failed",
                    decResult.ErrorMessage ?? "Unknown");
                return false;
            }
        }

        logger.VerificationCommandExecuted(runId, "Decrement x2");
        logger.VerificationStepComplete(runId, 1, "8 commands executed (1 init + 5 inc + 2 dec)");

        // Step 2: Read events directly from Cosmos to verify stream persistence
        logger.VerificationStep(runId, 2, "Read stream data directly from Cosmos");
        BrookPosition currentPosition = await brookStorageProvider.ReadCursorPositionAsync(brookKey, cancellationToken);
        logger.VerificationStreamCursor(runId, brookKey.Type, brookKey.Id, currentPosition.Value);
        if (currentPosition.Value < 8)
        {
            logger.VerificationStepFailed(
                runId,
                2,
                "Expected at least 8 events",
                $"Got cursor position {currentPosition.Value}");
            allPassed = false;
        }
        else
        {
            // Read all events
            int eventCount = 0;
            BrookRangeKey range = new(brookKey.Type, brookKey.Id, 1, currentPosition.Value);
            await foreach (BrookEvent evt in brookStorageProvider.ReadEventsAsync(range, cancellationToken))
            {
                eventCount++;
                if (eventCount <= 3)
                {
                    logger.VerificationEventRead(runId, eventCount, evt.EventType, evt.Data.Length);
                }
            }

            logger.VerificationEventCount(runId, eventCount);
            if (eventCount >= 8)
            {
                logger.VerificationStepComplete(runId, 2, $"Successfully read {eventCount} events from Cosmos");
            }
            else
            {
                logger.VerificationStepFailed(runId, 2, "Expected at least 8 events", $"Read only {eventCount} events");
                allPassed = false;
            }
        }

        // Step 3: Read snapshot directly from Cosmos to verify snapshot persistence
        logger.VerificationStep(runId, 3, "Read snapshot data directly from Cosmos");

        // Build the snapshot key: we need the projection type, projection id, reducer hash, and version
        // The version should match the current stream position
        string reducerHash = rootReducer.GetReducerHash();
        SnapshotStreamKey streamKey = new(brookKey.Type, counterId, reducerHash);
        SnapshotKey snapshotKey = new(streamKey, currentPosition.Value);
        SnapshotEnvelope? envelope = await snapshotStorageProvider.ReadAsync(snapshotKey, cancellationToken);
        if (envelope == null)
        {
            logger.VerificationSnapshotNotFound(runId, streamKey.ProjectionType, streamKey.ProjectionId);
            logger.VerificationStepFailed(runId, 3, "Snapshot not found", "ReadAsync returned null");
            allPassed = false;
        }
        else
        {
            logger.VerificationSnapshotFound(
                runId,
                streamKey.ProjectionType,
                streamKey.ProjectionId,
                snapshotKey.Version,
                envelope.Data.Length);

            // Deserialize the snapshot data using the state converter
            CounterState state = snapshotStateConverter.FromEnvelope(envelope);
            logger.VerificationSnapshotDetails(
                runId,
                envelope.DataContentType,
                envelope.ReducerHash,
                $"Count={state.Count}");

            // Expected final value: 10 (init) + 5 (increments) - 2 (decrements) = 13
            int expectedValue = 13;
            if (state.Count == expectedValue)
            {
                logger.VerificationStepComplete(
                    runId,
                    3,
                    $"Snapshot state Count {state.Count} matches expected {expectedValue}");
            }
            else
            {
                logger.VerificationStepFailed(runId, 3, $"Expected state Count {expectedValue}", $"Got {state.Count}");
                allPassed = false;
            }
        }

        sw.Stop();
        if (allPassed)
        {
            logger.VerificationScenarioComplete(runId, counterId, (int)sw.ElapsedMilliseconds, "PASSED");
        }
        else
        {
            logger.VerificationScenarioComplete(runId, counterId, (int)sw.ElapsedMilliseconds, "FAILED");
        }

        return allPassed;
    }
}