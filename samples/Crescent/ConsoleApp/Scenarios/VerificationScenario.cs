using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs end-to-end verification scenarios that validate event stream persistence.
/// </summary>
internal static class VerificationScenario
{
    private const string ScenarioName = "EndToEndVerification";

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
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunEndToEndVerificationAsync(
        ILogger logger,
        string runId,
        IGrainFactory grainFactory,
        IBrookStorageProvider brookStorageProvider,
        ISnapshotStorageProvider snapshotStorageProvider,
        ISnapshotStateConverter<CounterAggregate> snapshotStateConverter,
        IRootReducer<CounterAggregate> rootReducer,
        string counterId,
        CancellationToken cancellationToken = default
    )
    {
        logger.VerificationScenarioStart(runId, counterId);
        Stopwatch sw = Stopwatch.StartNew();
        bool allPassed = true;

        // Step 1: Build the grain key and perform aggregate operations
        BrookKey brookKey = new(BrookNames.Counter, counterId);
        ICounterAggregateGrain counter = grainFactory.GetGrain<ICounterAggregateGrain>(brookKey);
        logger.VerificationStep(runId, 1, "Execute aggregate commands to generate events");

        // Initialize with value 10
        OperationResult initResult = await counter.InitializeAsync(10);
        if (!initResult.Success)
        {
            logger.VerificationStepFailed(runId, 1, "Initialize failed", initResult.ErrorMessage ?? "Unknown");
            sw.Stop();
            return ScenarioResult.Failure(
                ScenarioName,
                initResult.ErrorMessage ?? "Initialize failed",
                (int)sw.ElapsedMilliseconds);
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
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    incResult.ErrorMessage ?? $"Increment[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
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
                sw.Stop();
                return ScenarioResult.Failure(
                    ScenarioName,
                    decResult.ErrorMessage ?? $"Decrement[{i + 1}] failed",
                    (int)sw.ElapsedMilliseconds);
            }
        }

        logger.VerificationCommandExecuted(runId, "Decrement x2");
        logger.VerificationStepComplete(runId, 1, "8 commands executed (1 init + 5 inc + 2 dec)");

        // Step 2: Read events directly from Cosmos to verify stream persistence
        logger.VerificationStep(runId, 2, "Read stream data directly from Cosmos");
        BrookPosition currentPosition = await brookStorageProvider.ReadCursorPositionAsync(brookKey, cancellationToken);
        logger.VerificationStreamCursor(runId, brookKey.BrookName, brookKey.EntityId, currentPosition.Value);

        // Cursor is 0-indexed: if we have 8 events (positions 0-7), cursor value is 7
        // So cursor >= 7 means at least 8 events
        if (currentPosition.Value < 7)
        {
            logger.VerificationStepFailed(
                runId,
                2,
                "Expected at least 8 events (cursor >= 7)",
                $"Got cursor position {currentPosition.Value}");
            allPassed = false;
        }
        else
        {
            // Read all events - start at position 0, count is cursor+1 (since cursor is 0-indexed)
            int eventCount = 0;
            BrookRangeKey range = new(brookKey.BrookName, brookKey.EntityId, 0, currentPosition.Value + 1);
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

        // Build the snapshot key: we need the brook name, projection type, projection id, reducer hash, and version
        // The version should match the current stream position
        string reducerHash = rootReducer.GetReducerHash();
        string projectionType = SnapshotStorageNameHelper.GetStorageName<CounterAggregate>();
        SnapshotStreamKey streamKey = new(brookKey.BrookName, projectionType, counterId, reducerHash);

        // Try to find the snapshot - it might be at a version less than currentPosition if persister hasn't caught up
        SnapshotEnvelope? envelope = null;
        long foundAtVersion = -1;

        // Try current position first, then work backwards (snapshots may lag behind events)
        for (long version = currentPosition.Value; (version >= 0) && (envelope == null); version--)
        {
            SnapshotKey snapshotKey = new(streamKey, version);
            envelope = await snapshotStorageProvider.ReadAsync(snapshotKey, cancellationToken);
            if (envelope != null)
            {
                foundAtVersion = version;
            }
        }

        if (envelope == null)
        {
            logger.VerificationSnapshotNotFound(runId, streamKey.SnapshotStorageName, streamKey.EntityId);
            logger.VerificationStepFailed(runId, 3, "Snapshot not found", "ReadAsync returned null for all versions");
            allPassed = false;
        }
        else
        {
            logger.VerificationSnapshotFound(
                runId,
                streamKey.SnapshotStorageName,
                streamKey.EntityId,
                foundAtVersion,
                envelope.Data.Length);

            // Deserialize the snapshot data using the state converter
            CounterAggregate state = snapshotStateConverter.FromEnvelope(envelope);
            logger.VerificationSnapshotDetails(
                runId,
                envelope.DataContentType,
                envelope.ReducerHash,
                $"Count={state.Count}");

            // Calculate expected value based on snapshot version (0-indexed)
            // Version 0 = after Init(10), Version 1-5 = after Increments, Version 6-7 = after Decrements
            // Version: 0=Init(10), 1=Inc(11), 2=Inc(12), 3=Inc(13), 4=Inc(14), 5=Inc(15), 6=Dec(14), 7=Dec(13)
            int expectedValue = foundAtVersion switch
            {
                0 => 10, // Just initialized
                1 => 11, // 1 increment
                2 => 12, // 2 increments
                3 => 13, // 3 increments
                4 => 14, // 4 increments
                5 => 15, // 5 increments
                6 => 14, // 5 increments + 1 decrement
                >= 7 => 13, // 5 increments + 2 decrements (final state)
                var _ => 13, // Default to final expected
            };
            if (state.Count == expectedValue)
            {
                logger.VerificationStepComplete(
                    runId,
                    3,
                    $"Snapshot state Count {state.Count} matches expected {expectedValue} for version {foundAtVersion}");
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
            return ScenarioResult.Success(ScenarioName, (int)sw.ElapsedMilliseconds, "All verification steps passed");
        }

        logger.VerificationScenarioComplete(runId, counterId, (int)sw.ElapsedMilliseconds, "FAILED");
        return ScenarioResult.Failure(
            ScenarioName,
            "One or more verification steps failed",
            (int)sw.ElapsedMilliseconds);
    }
}