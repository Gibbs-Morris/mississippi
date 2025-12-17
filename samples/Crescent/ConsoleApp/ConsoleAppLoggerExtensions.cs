using System;
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
        string errorMessage
    );

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
        string commandName
    );

    /// <summary>
    ///     Log that an aggregate scenario has completed.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="counterId">The counter identifier.</param>
    /// <param name="ms">The elapsed time in milliseconds.</param>
    [LoggerMessage(
        81,
        LogLevel.Information,
        "Run {RunId} [Aggregate:{ScenarioName}]: Complete for counterId={CounterId} in {Ms} ms")]
    public static partial void AggregateScenarioComplete(
        this ILogger logger,
        string runId,
        string scenarioName,
        string counterId,
        int ms
    );

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
        string note
    );

    /// <summary>
    ///     Log that an aggregate scenario has started.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="counterId">The counter identifier.</param>
    [LoggerMessage(
        80,
        LogLevel.Information,
        "Run {RunId} [Aggregate:{ScenarioName}]: Starting with counterId={CounterId}")]
    public static partial void AggregateScenarioStart(
        this ILogger logger,
        string runId,
        string scenarioName,
        string counterId
    );

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
    [LoggerMessage(
        85,
        LogLevel.Information,
        "Run {RunId}: Throughput test complete: total={Total} success={Success} failed={Failed} in {Ms} ms ({OpsPerSec} ops/sec)")]
    public static partial void AggregateThroughputResult(
        this ILogger logger,
        string runId,
        int total,
        int success,
        int failed,
        int ms,
        double opsPerSec
    );

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
    [LoggerMessage(
        31,
        LogLevel.Information,
        "Run {RunId} [{Scenario}]: Append complete -> cursor={Cursor} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)")]
    public static partial void AppendComplete(
        this ILogger logger,
        string runId,
        string scenario,
        long cursor,
        int ms,
        double rateEvN,
        double rateMB
    );

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
    [LoggerMessage(
        32,
        LogLevel.Error,
        "Run {RunId} [{Scenario}]: Append failed after {Ms} ms (attempted count={Count}, bytes={Bytes})")]
    public static partial void AppendFailed(
        this ILogger logger,
        string runId,
        string scenario,
        int ms,
        int count,
        long bytes,
        Exception ex
    );

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
        long bytes
    );

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
    [LoggerMessage(
        3,
        LogLevel.Information,
        "Run {RunId}: Cosmos options DatabaseId={DatabaseId}, ContainerId={ContainerId}, LockContainer={LockContainer}, MaxEventsPerBatch={MaxEventsPerBatch}, QueryBatchSize={QueryBatchSize}")]
    public static partial void CosmosOptions(
        this ILogger logger,
        string runId,
        string databaseId,
        string containerId,
        string lockContainer,
        int maxEventsPerBatch,
        int queryBatchSize
    );

    /// <summary>
    ///     Log the cursor position after the first write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position after the write.</param>
    [LoggerMessage(51, LogLevel.Information, "Run {RunId} [Interleave]: Cursor after write1={Cursor}")]
    public static partial void CursorAfterWrite1(
        this ILogger logger,
        string runId,
        long cursor
    );

    /// <summary>
    ///     Log the cursor position after the second write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position after the write.</param>
    [LoggerMessage(53, LogLevel.Information, "Run {RunId} [Interleave]: Cursor after write2={Cursor}")]
    public static partial void CursorAfterWrite2(
        this ILogger logger,
        string runId,
        long cursor
    );

    /// <summary>
    ///     Log the cursor positions for streams A and B in multi-stream scenarios.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursorA">The cursor position for stream A.</param>
    /// <param name="cursorB">The cursor position for stream B.</param>
    [LoggerMessage(60, LogLevel.Information, "Run {RunId} [Multi]: Cursors A={CursorA} B={CursorB}")]
    public static partial void CursorsAB(
        this ILogger logger,
        string runId,
        long cursorA,
        long cursorB
    );

    /// <summary>
    ///     Log the banner for the explicit cache flush and readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(18, LogLevel.Information, "=== Scenario: Explicit cache flush + readback ===")]
    public static partial void ExplicitCacheFlushReadback(
        this ILogger logger
    );

    /// <summary>
    ///     Log the full-range read count observed in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="count">The full-range read count.</param>
    [LoggerMessage(54, LogLevel.Information, "Run {RunId} [Interleave]: Full range read count={Count}")]
    public static partial void FullRangeReadCount(
        this ILogger logger,
        string runId,
        int count
    );

    /// <summary>
    ///     Log that the host has started for the provided run identifier.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(1, LogLevel.Information, "Run {RunId}: Host started")]
    public static partial void HostStarted(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log that the interleaved enumeration was aborted and will be retried once.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that caused the abort.</param>
    [LoggerMessage(55, LogLevel.Warning, "Run {RunId} [Interleave]: Enumeration aborted; retrying once")]
    public static partial void InterleaveEnumerationAbortedRetry(
        this ILogger logger,
        string runId,
        Exception ex
    );

    /// <summary>
    ///     Log the start of the interleaved scenario for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(50, LogLevel.Information, "Run {RunId} [Interleave]: Start")]
    public static partial void InterleaveStart(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log that the sample is running in fresh mode using a newly created <see cref="BrookKey" />.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The newly generated brook key.</param>
    /// <param name="path">The file path to the persisted state file.</param>
    [LoggerMessage(
        6,
        LogLevel.Information,
        "Run {RunId}: Mode=fresh, Using new BrookKey={BrookKey} (state file: {Path})")]
    public static partial void ModeFreshUsingNewBrookKey(
        this ILogger logger,
        string runId,
        BrookKey brookKey,
        string path
    );

    /// <summary>
    ///     Log that the sample is running in reuse mode with a persisted <see cref="BrookKey" />.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The persisted brook key being reused.</param>
    /// <param name="path">The file path to the persisted state file.</param>
    [LoggerMessage(
        5,
        LogLevel.Information,
        "Run {RunId}: Mode=reuse, Using persisted BrookKey={BrookKey} (state file: {Path})")]
    public static partial void ModeReuseUsingPersistedBrookKey(
        this ILogger logger,
        string runId,
        BrookKey brookKey,
        string path
    );

    /// <summary>
    ///     Log that there are no events to read for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(41, LogLevel.Information, "Run {RunId}: No events to read")]
    public static partial void NoEventsToRead(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log that the sample is performing a cold restart of the host.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(19, LogLevel.Information, "Run {RunId}: Performing cold restart of host...")]
    public static partial void PerformingColdRestartOfHost(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log the read counts for streams A and B in the multi-stream scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="countA">Read count for stream A.</param>
    /// <param name="countB">Read count for stream B.</param>
    [LoggerMessage(63, LogLevel.Information, "Run {RunId} [Multi]: Read counts A={CountA} B={CountB}")]
    public static partial void ReadCountsAB(
        this ILogger logger,
        string runId,
        int countA,
        int countB
    );

    /// <summary>
    ///     Log that a read enumeration was aborted and will be retried from the start.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that caused the abort.</param>
    [LoggerMessage(44, LogLevel.Warning, "Run {RunId}: Read enumeration aborted; retrying once from start")]
    public static partial void ReadEnumerationAbortedRetry(
        this ILogger logger,
        string runId,
        Exception ex
    );

    /// <summary>
    ///     Log details about a single event read during enumeration.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="idx">The index of the event within the current enumeration.</param>
    /// <param name="id">The event identifier.</param>
    /// <param name="type">The CLR type name or event type marker.</param>
    /// <param name="bytes">The size in bytes of the event payload.</param>
    [LoggerMessage(42, LogLevel.Debug, "Run {RunId}: Read idx={Idx} id={Id} type={Type} bytes={Bytes}")]
    public static partial void ReadIdxEvent(
        this ILogger logger,
        string runId,
        int idx,
        string id,
        string type,
        int bytes
    );

    /// <summary>
    ///     Log the banner for the readback that follows initial appends.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(15, LogLevel.Information, "=== Readback after initial appends ===")]
    public static partial void ReadbackAfterInitialAppends(
        this ILogger logger
    );

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
    [LoggerMessage(
        43,
        LogLevel.Information,
        "Run {RunId}: Readback complete count={Count} bytes={Bytes} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)")]
    public static partial void ReadbackComplete(
        this ILogger logger,
        string runId,
        int count,
        long bytes,
        int ms,
        double rateEvN,
        double rateMB
    );

    /// <summary>
    ///     Log the current readback cursor position for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="cursor">The cursor position observed during readback.</param>
    [LoggerMessage(40, LogLevel.Information, "Run {RunId}: Readback cursor={Cursor}")]
    public static partial void ReadbackCursor(
        this ILogger logger,
        string runId,
        long cursor
    );

    /// <summary>
    ///     Log that the host is requesting grain deactivations for the specified brook key.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="brookKey">The brook key for which deactivations are requested.</param>
    [LoggerMessage(70, LogLevel.Information, "Run {RunId} [Flush]: Requesting grain deactivations for {BrookKey}")]
    public static partial void RequestingDeactivations(
        this ILogger logger,
        string runId,
        BrookKey brookKey
    );

    /// <summary>
    ///     Log that the Orleans grain factory is being resolved for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(2, LogLevel.Information, "Run {RunId}: Resolving Orleans grain factory")]
    public static partial void ResolvingGrainFactory(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log the banner for the aggregate basic lifecycle scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(86, LogLevel.Information, "=== Scenario: Aggregate Basic Lifecycle ===")]
    public static partial void ScenarioAggregateBasicLifecycle(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the aggregate concurrency scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(89, LogLevel.Information, "=== Scenario: Aggregate Concurrency ===")]
    public static partial void ScenarioAggregateConcurrency(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the aggregate throughput scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(88, LogLevel.Information, "=== Scenario: Aggregate Throughput ===")]
    public static partial void ScenarioAggregateThroughput(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the aggregate validation errors scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(87, LogLevel.Information, "=== Scenario: Aggregate Validation Errors ===")]
    public static partial void ScenarioAggregateValidation(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the Bulk_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(11, LogLevel.Information, "=== Scenario: Bulk_100_Mixed ===")]
    public static partial void ScenarioBulk100Mixed(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the cold restart readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(20, LogLevel.Information, "=== Scenario: Cold restart readback ===")]
    public static partial void ScenarioColdRestartReadback(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the interleaved read/write scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(16, LogLevel.Information, "=== Scenario: Interleaved Read/Write (single stream) ===")]
    public static partial void ScenarioInterleaved(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the LargeBatch_200x5KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(13, LogLevel.Information, "=== Scenario: LargeBatch_200x5KB ===")]
    public static partial void ScenarioLargeBatch200x5KB(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the LargeSingle_200KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(12, LogLevel.Information, "=== Scenario: LargeSingle_200KB ===")]
    public static partial void ScenarioLargeSingle200KB(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the multi-stream interleaved workload scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(17, LogLevel.Information, "=== Scenario: Multi-stream interleaved workload ===")]
    public static partial void ScenarioMultiStream(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the OpsLimit_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(14, LogLevel.Information, "=== Scenario: OpsLimit_100_Mixed ===")]
    public static partial void ScenarioOpsLimit100Mixed(
        this ILogger logger
    );

    /// <summary>
    ///     Log the banner for the SmallBatch_10x1KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    [LoggerMessage(10, LogLevel.Information, "=== Scenario: SmallBatch_10x1KB ===")]
    public static partial void ScenarioSmallBatch10x1KB(
        this ILogger logger
    );

    /// <summary>
    ///     Log that stream A is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(61, LogLevel.Information, "Run {RunId} [Multi]: Stream A empty")]
    public static partial void StreamAEmpty(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log that stream B is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    [LoggerMessage(62, LogLevel.Information, "Run {RunId} [Multi]: Stream B empty")]
    public static partial void StreamBEmpty(
        this ILogger logger,
        string runId
    );

    /// <summary>
    ///     Log the count of entries read from the tail in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="count">The tail read count.</param>
    [LoggerMessage(52, LogLevel.Information, "Run {RunId} [Interleave]: Tail read count={Count}")]
    public static partial void TailReadCount(
        this ILogger logger,
        string runId,
        int count
    );

    /// <summary>
    ///     Log that BrookStorageOptions could not be resolved for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="ex">The exception that prevented resolution.</param>
    [LoggerMessage(4, LogLevel.Warning, "Run {RunId}: Unable to resolve BrookStorageOptions")]
    public static partial void UnableToResolveBrookStorageOptions(
        this ILogger logger,
        string runId,
        Exception ex
    );
}