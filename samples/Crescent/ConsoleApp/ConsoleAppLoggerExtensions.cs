using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;

namespace Crescent.ConsoleApp;

/// <summary>
///     LoggerMessage-based high-performance logging extensions for the Crescent console sample.
/// </summary>
internal static partial class ConsoleAppLoggerExtensions
{

    /// <summary>
    ///     Log that an aggregate command failed.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="commandName">The name of the command that failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(83, LogLevel.Warning, "Run {RunId}: Command {CommandName} failed: {ErrorCode} - {ErrorMessage}")]
    public static partial void AggregateCommandFailed(
        this ILogger logger,
        string runId,
        string commandName,
        string errorCode,
        string errorMessage);

    /// <summary>
    ///     Log that an aggregate command succeeded.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="commandName">The name of the command that succeeded.</param>
    [LoggerMessage(82, LogLevel.Debug, "Run {RunId}: Command {CommandName} succeeded")]
    public static partial void AggregateCommandSuccess(
        this ILogger logger,
        string runId,
        string commandName);

    /// <summary>
    ///     Log that an aggregate scenario has completed.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="counterId">The counter identifier.</param>
    /// <param name="ms">The elapsed time in milliseconds.</param>
    [LoggerMessage(81, LogLevel.Information, "Run {RunId} [Aggregate:{ScenarioName}]: Complete for counterId={CounterId} in {Ms} ms")]
    public static partial void AggregateScenarioComplete(
        this ILogger logger,
        string runId,
        string scenarioName,
        string counterId,
        int ms);

    /// <summary>
    ///     Log a note during an aggregate scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="note">The note to log.</param>
    [LoggerMessage(84, LogLevel.Information, "Run {RunId}: Note: {Note}")]
    public static partial void AggregateScenarioNote(
        this ILogger logger,
        string runId,
        string note);

    /// <summary>
    ///     Log that an aggregate scenario has started.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="counterId">The counter identifier.</param>
    [LoggerMessage(80, LogLevel.Information, "Run {RunId} [Aggregate:{ScenarioName}]: Starting with counterId={CounterId}")]
    public static partial void AggregateScenarioStart(
        this ILogger logger,
        string runId,
        string scenarioName,
        string counterId);

    /// <summary>
    ///     Log the result of a throughput test.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="total">Total number of operations attempted.</param>
    /// <param name="success">Number of successful operations.</param>
    /// <param name="failed">Number of failed operations.</param>
    /// <param name="ms">The elapsed time in milliseconds.</param>
    /// <param name="opsPerSec">Operations per second throughput.</param>
    [LoggerMessage(85, LogLevel.Information, "Run {RunId}: Throughput test complete: total={Total} success={Success} failed={Failed} in {Ms} ms ({OpsPerSec} ops/sec)")]
    public static partial void AggregateThroughputResult(
        this ILogger logger,
        string runId,
        int total,
        int success,
        int failed,
        int ms,
        double opsPerSec);

    /// <summary>
    ///     Log metrics for a completed append operation.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenario">The scenario name for the append operation.</param>
    /// <param name="cursor">The resulting cursor position after the append.</param>
    /// <param name="ms">The elapsed time in milliseconds for the append.</param>
    /// <param name="rateEvN">Event throughput in events/second.</param>
    /// <param name="rateMB">Data throughput in MB/second.</param>
    [LoggerMessage(31, LogLevel.Information, "Run {RunId} [{Scenario}]: Append complete -> cursor={Cursor} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)")]
    public static partial void AppendComplete(
        this ILogger logger,
        string runId,
        string scenario,
        long cursor,
        int ms,
        double rateEvN,
        double rateMB);

    /// <summary>
    ///     Log a failed append operation along with failure metrics.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenario">The scenario name for the append operation.</param>
    /// <param name="ms">The elapsed time in milliseconds when the failure occurred.</param>
    /// <param name="count">The attempted number of events for the append.</param>
    /// <param name="bytes">The attempted number of bytes for the append.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    [LoggerMessage(32, LogLevel.Error, "Run {RunId} [{Scenario}]: Append failed after {Ms} ms (attempted count={Count}, bytes={Bytes})")]
    public static partial void AppendFailed(
        this ILogger logger,
        string runId,
        string scenario,
        int ms,
        int count,
        long bytes,
        Exception ex);

    /// <summary>
    ///     Log information about the number of events being appended.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenario">The scenario name for the append operation.</param>
    /// <param name="count">The number of events being appended.</param>
    /// <param name="bytes">The total number of bytes being appended.</param>
    [LoggerMessage(30, LogLevel.Information, "Run {RunId} [{Scenario}]: Appending count={Count} totalBytes={Bytes}")]
    public static partial void AppendingCounts(
        this ILogger logger,
        string runId,
        string scenario,
        int count,
        long bytes);

    /// <summary>
    ///     Log the Cosmos DB configuration options used by the sample.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="databaseId">The configured Cosmos database id.</param>
    /// <param name="containerId">The configured Cosmos container id.</param>
    /// <param name="lockContainer">The container used for locks.</param>
    /// <param name="maxEventsPerBatch">Maximum events per batch for append operations.</param>
    /// <param name="queryBatchSize">Batch size used when querying events.</param>
    public static void CosmosOptions(
        this ILogger logger,
        string runId,
        string databaseId,
        string containerId,
        string lockContainer,
        int maxEventsPerBatch,
        int queryBatchSize
    ) =>
        CosmosOptionsMessage(
            logger,
            runId,
            databaseId,
            containerId,
            lockContainer,
            maxEventsPerBatch,
            queryBatchSize,
            null);

    /// <summary>
    ///     Log the cursor position after the first write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position after the write.</param>
    public static void CursorAfterWrite1(
        this ILogger logger,
        string runId,
        long cursor
    ) =>
        CursorAfterWrite1Message(logger, runId, cursor, null);

    /// <summary>
    ///     Log the cursor position after the second write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position after the write.</param>
    public static void CursorAfterWrite2(
        this ILogger logger,
        string runId,
        long cursor
    ) =>
        CursorAfterWrite2Message(logger, runId, cursor, null);

    /// <summary>
    ///     Log the cursor positions for streams A and B in multi-stream scenarios.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursorA">The cursor position for stream A.</param>
    /// <param name="cursorB">The cursor position for stream B.</param>
    public static void CursorsAB(
        this ILogger logger,
        string runId,
        long cursorA,
        long cursorB
    ) =>
        CursorsABMessage(logger, runId, cursorA, cursorB, null);

    /// <summary>
    ///     Log the banner for the explicit cache flush and readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ExplicitCacheFlushReadback(
        this ILogger logger
    ) =>
        ExplicitCacheFlushReadbackMessage(logger, null);

    /// <summary>
    ///     Log the full-range read count observed in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="count">The full-range read count.</param>
    public static void FullRangeReadCount(
        this ILogger logger,
        string runId,
        int count
    ) =>
        FullRangeReadCountMessage(logger, runId, count, null);

    /// <summary>
    ///     Log that the host has started for the provided run identifier.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void HostStarted(
        this ILogger logger,
        string runId
    ) =>
        HostStartedMessage(logger, runId, null);

    /// <summary>
    ///     Log that the interleaved enumeration was aborted and will be retried once.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that caused the abort.</param>
    public static void InterleaveEnumerationAbortedRetry(
        this ILogger logger,
        string runId,
        Exception ex
    ) =>
        InterleaveEnumerationAbortedRetryMessage(logger, runId, ex);

    /// <summary>
    ///     Log the start of the interleaved scenario for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void InterleaveStart(
        this ILogger logger,
        string runId
    ) =>
        InterleaveStartMessage(logger, runId, null);

    /// <summary>
    ///     Log that the sample is running in fresh mode using a newly created <see cref="BrookKey" />.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The newly generated brook key.</param>
    /// <param name="path">The file path to the persisted state file.</param>
    public static void ModeFreshUsingNewBrookKey(
        this ILogger logger,
        string runId,
        BrookKey brookKey,
        string path
    ) =>
        ModeFreshUsingNewBrookKeyMessage(logger, runId, brookKey, path, null);

    /// <summary>
    ///     Log that the sample is running in reuse mode with a persisted <see cref="BrookKey" />.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The persisted brook key being reused.</param>
    /// <param name="path">The file path to the persisted state file.</param>
    public static void ModeReuseUsingPersistedBrookKey(
        this ILogger logger,
        string runId,
        BrookKey brookKey,
        string path
    ) =>
        ModeReuseUsingPersistedBrookKeyMessage(logger, runId, brookKey, path, null);

    /// <summary>
    ///     Log that there are no events to read for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void NoEventsToRead(
        this ILogger logger,
        string runId
    ) =>
        NoEventsToReadMessage(logger, runId, null);

    /// <summary>
    ///     Log that the sample is performing a cold restart of the host.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void PerformingColdRestartOfHost(
        this ILogger logger,
        string runId
    ) =>
        PerformingColdRestartOfHostMessage(logger, runId, null);

    /// <summary>
    ///     Log the read counts for streams A and B in the multi-stream scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="countA">Read count for stream A.</param>
    /// <param name="countB">Read count for stream B.</param>
    public static void ReadCountsAB(
        this ILogger logger,
        string runId,
        int countA,
        int countB
    ) =>
        ReadCountsABMessage(logger, runId, countA, countB, null);

    /// <summary>
    ///     Log that a read enumeration was aborted and will be retried from the start.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that caused the abort.</param>
    public static void ReadEnumerationAbortedRetry(
        this ILogger logger,
        string runId,
        Exception ex
    ) =>
        ReadEnumerationAbortedRetryMessage(logger, runId, ex);

    /// <summary>
    ///     Log details about a single event read during enumeration.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="idx">The index of the event within the current enumeration.</param>
    /// <param name="id">The event identifier.</param>
    /// <param name="type">The CLR type name or event type marker.</param>
    /// <param name="bytes">The size in bytes of the event payload.</param>
    public static void ReadIdxEvent(
        this ILogger logger,
        string runId,
        int idx,
        string id,
        string type,
        int bytes
    ) =>
        ReadIdxEventMessage(logger, runId, idx, id, type, bytes, null);

    /// <summary>
    ///     Log the banner for the readback that follows initial appends.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ReadbackAfterInitialAppends(
        this ILogger logger
    ) =>
        ReadbackAfterInitialAppendsMessage(logger, null);

    /// <summary>
    ///     Log metrics when a readback operation completes.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="count">The number of events read.</param>
    /// <param name="bytes">The total bytes read.</param>
    /// <param name="ms">Elapsed time in milliseconds for the readback.</param>
    /// <param name="rateEvN">Event throughput in events/second.</param>
    /// <param name="rateMB">Data throughput in MB/second.</param>
    public static void ReadbackComplete(
        this ILogger logger,
        string runId,
        int count,
        long bytes,
        int ms,
        double rateEvN,
        double rateMB
    ) =>
        ReadbackCompleteMessage(logger, runId, count, bytes, ms, rateEvN, rateMB, null);

    /// <summary>
    ///     Log the current readback cursor position for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position observed during readback.</param>
    public static void ReadbackCursor(
        this ILogger logger,
        string runId,
        long cursor
    ) =>
        ReadbackCursorMessage(logger, runId, cursor, null);

    /// <summary>
    ///     Log that the host is requesting grain deactivations for the specified brook key.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The brook key for which deactivations are requested.</param>
    public static void RequestingDeactivations(
        this ILogger logger,
        string runId,
        BrookKey brookKey
    ) =>
        RequestingDeactivationsMessage(logger, runId, brookKey, null);

    /// <summary>
    ///     Log that the Orleans grain factory is being resolved for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void ResolvingGrainFactory(
        this ILogger logger,
        string runId
    ) =>
        ResolvingGrainFactoryMessage(logger, runId, null);

    /// <summary>
    ///     Log the banner for the aggregate basic lifecycle scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioAggregateBasicLifecycle(
        this ILogger logger
    ) =>
        ScenarioAggregateBasicLifecycleMessage(logger, null);

    /// <summary>
    ///     Log the banner for the aggregate concurrency scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioAggregateConcurrency(
        this ILogger logger
    ) =>
        ScenarioAggregateConcurrencyMessage(logger, null);

    /// <summary>
    ///     Log the banner for the aggregate throughput scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioAggregateThroughput(
        this ILogger logger
    ) =>
        ScenarioAggregateThroughputMessage(logger, null);

    /// <summary>
    ///     Log the banner for the aggregate validation errors scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioAggregateValidation(
        this ILogger logger
    ) =>
        ScenarioAggregateValidationMessage(logger, null);

    /// <summary>
    ///     Log the banner for the Bulk_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioBulk100Mixed(
        this ILogger logger
    ) =>
        ScenarioBulk100MixedMessage(logger, null);

    /// <summary>
    ///     Log the banner for the cold restart readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioColdRestartReadback(
        this ILogger logger
    ) =>
        ScenarioColdRestartReadbackMessage(logger, null);

    /// <summary>
    ///     Log the banner for the interleaved read/write scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioInterleaved(
        this ILogger logger
    ) =>
        ScenarioInterleavedMessage(logger, null);

    /// <summary>
    ///     Log the banner for the LargeBatch_200x5KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioLargeBatch200x5KB(
        this ILogger logger
    ) =>
        ScenarioLargeBatch200x5KBMessage(logger, null);

    /// <summary>
    ///     Log the banner for the LargeSingle_200KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioLargeSingle200KB(
        this ILogger logger
    ) =>
        ScenarioLargeSingle200KBMessage(logger, null);

    /// <summary>
    ///     Log the banner for the multi-stream interleaved workload scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioMultiStream(
        this ILogger logger
    ) =>
        ScenarioMultiStreamMessage(logger, null);

    /// <summary>
    ///     Log the banner for the OpsLimit_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioOpsLimit100Mixed(
        this ILogger logger
    ) =>
        ScenarioOpsLimit100MixedMessage(logger, null);

    /// <summary>
    ///     Log the banner for the SmallBatch_10x1KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioSmallBatch10x1KB(
        this ILogger logger
    ) =>
        ScenarioSmallBatch10x1KBMessage(logger, null);

    /// <summary>
    ///     Log that stream A is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void StreamAEmpty(
        this ILogger logger,
        string runId
    ) =>
        StreamAEmptyMessage(logger, runId, null);

    /// <summary>
    ///     Log that stream B is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void StreamBEmpty(
        this ILogger logger,
        string runId
    ) =>
        StreamBEmptyMessage(logger, runId, null);

    /// <summary>
    ///     Log the count of entries read from the tail in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="count">The tail read count.</param>
    public static void TailReadCount(
        this ILogger logger,
        string runId,
        int count
    ) =>
        TailReadCountMessage(logger, runId, count, null);

    /// <summary>
    ///     Log that BrookStorageOptions could not be resolved for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that prevented resolution.</param>
    public static void UnableToResolveBrookStorageOptions(
        this ILogger logger,
        string runId,
        Exception ex
    ) =>
        UnableToResolveBrookStorageOptionsMessage(logger, runId, ex);
}