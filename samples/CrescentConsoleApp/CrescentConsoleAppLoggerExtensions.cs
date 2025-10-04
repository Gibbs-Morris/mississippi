using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     LoggerMessage-based high-performance logging extensions for the Crescent console sample.
/// </summary>
internal static class CrescentConsoleAppLoggerExtensions
{
    // Top-level lifecycle and setup
    private static readonly Action<ILogger, string, Exception?> HostStartedMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new(1, nameof(HostStarted)),
        "Run {RunId}: Host started");

    private static readonly Action<ILogger, string, Exception?> ResolvingGrainFactoryMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new(2, nameof(ResolvingGrainFactory)),
            "Run {RunId}: Resolving Orleans grain factory");

    private static readonly Action<ILogger, string, string, string, string, int, int, Exception?> CosmosOptionsMessage =
        LoggerMessage.Define<string, string, string, string, int, int>(
            LogLevel.Information,
            new(3, nameof(CosmosOptions)),
            "Run {RunId}: Cosmos options DatabaseId={DatabaseId}, ContainerId={ContainerId}, LockContainer={LockContainer}, MaxEventsPerBatch={MaxEventsPerBatch}, QueryBatchSize={QueryBatchSize}");

    private static readonly Action<ILogger, string, Exception> UnableToResolveBrookStorageOptionsMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new(4, nameof(UnableToResolveBrookStorageOptions)),
            "Run {RunId}: Unable to resolve BrookStorageOptions");

    private static readonly Action<ILogger, string, BrookKey, string, Exception?>
        ModeReuseUsingPersistedBrookKeyMessage = LoggerMessage.Define<string, BrookKey, string>(
            LogLevel.Information,
            new(5, nameof(ModeReuseUsingPersistedBrookKey)),
            "Run {RunId}: Mode=reuse, Using persisted BrookKey={BrookKey} (state file: {Path})");

    private static readonly Action<ILogger, string, BrookKey, string, Exception?> ModeFreshUsingNewBrookKeyMessage =
        LoggerMessage.Define<string, BrookKey, string>(
            LogLevel.Information,
            new(6, nameof(ModeFreshUsingNewBrookKey)),
            "Run {RunId}: Mode=fresh, Using new BrookKey={BrookKey} (state file: {Path})");

    // Scenario banners
    private static readonly Action<ILogger, Exception?> ScenarioSmallBatch10x1KBMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(10, nameof(ScenarioSmallBatch10x1KB)),
        "=== Scenario: SmallBatch_10x1KB ===");

    private static readonly Action<ILogger, Exception?> ScenarioBulk100MixedMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(11, nameof(ScenarioBulk100Mixed)),
        "=== Scenario: Bulk_100_Mixed ===");

    private static readonly Action<ILogger, Exception?> ScenarioLargeSingle200KBMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(12, nameof(ScenarioLargeSingle200KB)),
        "=== Scenario: LargeSingle_200KB ===");

    private static readonly Action<ILogger, Exception?> ScenarioLargeBatch200x5KBMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(13, nameof(ScenarioLargeBatch200x5KB)),
        "=== Scenario: LargeBatch_200x5KB ===");

    private static readonly Action<ILogger, Exception?> ScenarioOpsLimit100MixedMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(14, nameof(ScenarioOpsLimit100Mixed)),
        "=== Scenario: OpsLimit_100_Mixed ===");

    private static readonly Action<ILogger, Exception?> ReadbackAfterInitialAppendsMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(15, nameof(ReadbackAfterInitialAppends)),
        "=== Readback after initial appends ===");

    private static readonly Action<ILogger, Exception?> ScenarioInterleavedMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(16, nameof(ScenarioInterleaved)),
        "=== Scenario: Interleaved Read/Write (single stream) ===");

    private static readonly Action<ILogger, Exception?> ScenarioMultiStreamMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(17, nameof(ScenarioMultiStream)),
        "=== Scenario: Multi-stream interleaved workload ===");

    private static readonly Action<ILogger, Exception?> ExplicitCacheFlushReadbackMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(18, nameof(ExplicitCacheFlushReadback)),
        "=== Scenario: Explicit cache flush + readback ===");

    private static readonly Action<ILogger, string, Exception?> PerformingColdRestartOfHostMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new(19, nameof(PerformingColdRestartOfHost)),
            "Run {RunId}: Performing cold restart of host...");

    private static readonly Action<ILogger, Exception?> ScenarioColdRestartReadbackMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(20, nameof(ScenarioColdRestartReadback)),
        "=== Scenario: Cold restart readback ===");

    // Append scenario
    private static readonly Action<ILogger, string, string, int, long, Exception?> AppendingCountsMessage =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Information,
            new(30, nameof(AppendingCounts)),
            "Run {RunId} [{Scenario}]: Appending count={Count} totalBytes={Bytes}");

    private static readonly Action<ILogger, string, string, long, int, double, double, Exception?>
        AppendCompleteMessage = LoggerMessage.Define<string, string, long, int, double, double>(
            LogLevel.Information,
            new(31, nameof(AppendComplete)),
            "Run {RunId} [{Scenario}]: Append complete -> head={Head} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)");

    private static readonly Action<ILogger, string, string, int, int, long, Exception> AppendFailedMessage =
        LoggerMessage.Define<string, string, int, int, long>(
            LogLevel.Error,
            new(32, nameof(AppendFailed)),
            "Run {RunId} [{Scenario}]: Append failed after {Ms} ms (attempted count={Count}, bytes={Bytes})");

    // Readback
    private static readonly Action<ILogger, string, long, Exception?> ReadbackHeadMessage =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new(40, nameof(ReadbackHead)),
            "Run {RunId}: Readback head={Head}");

    private static readonly Action<ILogger, string, Exception?> NoEventsToReadMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new(41, nameof(NoEventsToRead)),
        "Run {RunId}: No events to read");

    private static readonly Action<ILogger, string, int, string, string, int, Exception?> ReadIdxEventMessage =
        LoggerMessage.Define<string, int, string, string, int>(
            LogLevel.Debug,
            new(42, nameof(ReadIdxEvent)),
            "Run {RunId}: Read idx={Idx} id={Id} type={Type} bytes={Bytes}");

    private static readonly Action<ILogger, string, int, long, int, double, double, Exception?>
        ReadbackCompleteMessage = LoggerMessage.Define<string, int, long, int, double, double>(
            LogLevel.Information,
            new(43, nameof(ReadbackComplete)),
            "Run {RunId}: Readback complete count={Count} bytes={Bytes} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)");

    private static readonly Action<ILogger, string, Exception> ReadEnumerationAbortedRetryMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new(44, nameof(ReadEnumerationAbortedRetry)),
            "Run {RunId}: Read enumeration aborted; retrying once from start");

    // Interleaved scenario
    private static readonly Action<ILogger, string, Exception?> InterleaveStartMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new(50, nameof(InterleaveStart)),
        "Run {RunId} [Interleave]: Start");

    private static readonly Action<ILogger, string, long, Exception?> HeadAfterWrite1Message =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new(51, nameof(HeadAfterWrite1)),
            "Run {RunId} [Interleave]: Head after write1={Head}");

    private static readonly Action<ILogger, string, int, Exception?> TailReadCountMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new(52, nameof(TailReadCount)),
            "Run {RunId} [Interleave]: Tail read count={Count}");

    private static readonly Action<ILogger, string, long, Exception?> HeadAfterWrite2Message =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new(53, nameof(HeadAfterWrite2)),
            "Run {RunId} [Interleave]: Head after write2={Head}");

    private static readonly Action<ILogger, string, int, Exception?> FullRangeReadCountMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new(54, nameof(FullRangeReadCount)),
            "Run {RunId} [Interleave]: Full range read count={Count}");

    private static readonly Action<ILogger, string, Exception> InterleaveEnumerationAbortedRetryMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new(55, nameof(InterleaveEnumerationAbortedRetry)),
            "Run {RunId} [Interleave]: Enumeration aborted; retrying once");

    // Multi-stream scenario
    private static readonly Action<ILogger, string, long, long, Exception?> HeadsABMessage =
        LoggerMessage.Define<string, long, long>(
            LogLevel.Information,
            new(60, nameof(HeadsAB)),
            "Run {RunId} [Multi]: Heads A={HA} B={HB}");

    private static readonly Action<ILogger, string, Exception?> StreamAEmptyMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new(61, nameof(StreamAEmpty)),
        "Run {RunId} [Multi]: Stream A empty");

    private static readonly Action<ILogger, string, Exception?> StreamBEmptyMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new(62, nameof(StreamBEmpty)),
        "Run {RunId} [Multi]: Stream B empty");

    private static readonly Action<ILogger, string, int, int, Exception?> ReadCountsABMessage =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Information,
            new(63, nameof(ReadCountsAB)),
            "Run {RunId} [Multi]: Read counts A={CA} B={CB}");

    // Flush caches
    private static readonly Action<ILogger, string, BrookKey, Exception?> RequestingDeactivationsMessage =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Information,
            new(70, nameof(RequestingDeactivations)),
            "Run {RunId} [Flush]: Requesting grain deactivations for {BrookKey}");

    /// <summary>
    ///     Log that the host has started for the provided run identifier.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void HostStarted(
        this ILogger logger,
        string runId
    )
    {
        HostStartedMessage(logger, runId, null);
    }

    /// <summary>
    ///     Log that the Orleans grain factory is being resolved for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void ResolvingGrainFactory(
        this ILogger logger,
        string runId
    )
    {
        ResolvingGrainFactoryMessage(logger, runId, null);
    }

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
    )
    {
        CosmosOptionsMessage(
            logger,
            runId,
            databaseId,
            containerId,
            lockContainer,
            maxEventsPerBatch,
            queryBatchSize,
            null);
    }

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
    )
    {
        UnableToResolveBrookStorageOptionsMessage(logger, runId, ex);
    }

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
    )
    {
        ModeReuseUsingPersistedBrookKeyMessage(logger, runId, brookKey, path, null);
    }

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
    )
    {
        ModeFreshUsingNewBrookKeyMessage(logger, runId, brookKey, path, null);
    }

    /// <summary>
    ///     Log the banner for the SmallBatch_10x1KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioSmallBatch10x1KB(
        this ILogger logger
    )
    {
        ScenarioSmallBatch10x1KBMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the Bulk_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioBulk100Mixed(
        this ILogger logger
    )
    {
        ScenarioBulk100MixedMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the LargeSingle_200KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioLargeSingle200KB(
        this ILogger logger
    )
    {
        ScenarioLargeSingle200KBMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the LargeBatch_200x5KB scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioLargeBatch200x5KB(
        this ILogger logger
    )
    {
        ScenarioLargeBatch200x5KBMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the OpsLimit_100_Mixed scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioOpsLimit100Mixed(
        this ILogger logger
    )
    {
        ScenarioOpsLimit100MixedMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the readback that follows initial appends.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ReadbackAfterInitialAppends(
        this ILogger logger
    )
    {
        ReadbackAfterInitialAppendsMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the interleaved read/write scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioInterleaved(
        this ILogger logger
    )
    {
        ScenarioInterleavedMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the multi-stream interleaved workload scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioMultiStream(
        this ILogger logger
    )
    {
        ScenarioMultiStreamMessage(logger, null);
    }

    /// <summary>
    ///     Log the banner for the explicit cache flush and readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ExplicitCacheFlushReadback(
        this ILogger logger
    )
    {
        ExplicitCacheFlushReadbackMessage(logger, null);
    }

    /// <summary>
    ///     Log that the sample is performing a cold restart of the host.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void PerformingColdRestartOfHost(
        this ILogger logger,
        string runId
    )
    {
        PerformingColdRestartOfHostMessage(logger, runId, null);
    }

    /// <summary>
    ///     Log the banner for the cold restart readback scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    public static void ScenarioColdRestartReadback(
        this ILogger logger
    )
    {
        ScenarioColdRestartReadbackMessage(logger, null);
    }

    /// <summary>
    ///     Log information about the number of events being appended.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenario">The scenario name for the append operation.</param>
    /// <param name="count">The number of events being appended.</param>
    /// <param name="bytes">The total number of bytes being appended.</param>
    public static void AppendingCounts(
        this ILogger logger,
        string runId,
        string scenario,
        int count,
        long bytes
    )
    {
        AppendingCountsMessage(logger, runId, scenario, count, bytes, null);
    }

    /// <summary>
    ///     Log metrics for a completed append operation.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="scenario">The scenario name for the append operation.</param>
    /// <param name="head">The resulting head position after the append.</param>
    /// <param name="ms">The elapsed time in milliseconds for the append.</param>
    /// <param name="rateEvN">Event throughput in events/second.</param>
    /// <param name="rateMB">Data throughput in MB/second.</param>
    public static void AppendComplete(
        this ILogger logger,
        string runId,
        string scenario,
        long head,
        int ms,
        double rateEvN,
        double rateMB
    )
    {
        AppendCompleteMessage(logger, runId, scenario, head, ms, rateEvN, rateMB, null);
    }

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
    public static void AppendFailed(
        this ILogger logger,
        string runId,
        string scenario,
        int ms,
        int count,
        long bytes,
        Exception ex
    )
    {
        AppendFailedMessage(logger, runId, scenario, ms, count, bytes, ex);
    }

    /// <summary>
    ///     Log the current readback head position for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="head">The head position observed during readback.</param>
    public static void ReadbackHead(
        this ILogger logger,
        string runId,
        long head
    )
    {
        ReadbackHeadMessage(logger, runId, head, null);
    }

    /// <summary>
    ///     Log that there are no events to read for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void NoEventsToRead(
        this ILogger logger,
        string runId
    )
    {
        NoEventsToReadMessage(logger, runId, null);
    }

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
    )
    {
        ReadIdxEventMessage(logger, runId, idx, id, type, bytes, null);
    }

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
    )
    {
        ReadbackCompleteMessage(logger, runId, count, bytes, ms, rateEvN, rateMB, null);
    }

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
    )
    {
        ReadEnumerationAbortedRetryMessage(logger, runId, ex);
    }

    /// <summary>
    ///     Log the start of the interleaved scenario for the run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void InterleaveStart(
        this ILogger logger,
        string runId
    )
    {
        InterleaveStartMessage(logger, runId, null);
    }

    /// <summary>
    ///     Log the head position after the first write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="head">The head position after the write.</param>
    public static void HeadAfterWrite1(
        this ILogger logger,
        string runId,
        long head
    )
    {
        HeadAfterWrite1Message(logger, runId, head, null);
    }

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
    )
    {
        TailReadCountMessage(logger, runId, count, null);
    }

    /// <summary>
    ///     Log the head position after the second write in the interleaved scenario.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="head">The head position after the write.</param>
    public static void HeadAfterWrite2(
        this ILogger logger,
        string runId,
        long head
    )
    {
        HeadAfterWrite2Message(logger, runId, head, null);
    }

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
    )
    {
        FullRangeReadCountMessage(logger, runId, count, null);
    }

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
    )
    {
        InterleaveEnumerationAbortedRetryMessage(logger, runId, ex);
    }

    /// <summary>
    ///     Log the head positions for streams A and B in multi-stream scenarios.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    /// <param name="headA">The head position for stream A.</param>
    /// <param name="headB">The head position for stream B.</param>
    public static void HeadsAB(
        this ILogger logger,
        string runId,
        long headA,
        long headB
    )
    {
        HeadsABMessage(logger, runId, headA, headB, null);
    }

    /// <summary>
    ///     Log that stream A is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void StreamAEmpty(
        this ILogger logger,
        string runId
    )
    {
        StreamAEmptyMessage(logger, runId, null);
    }

    /// <summary>
    ///     Log that stream B is empty for the given run.
    /// </summary>
    /// <param name="logger">The logger used to write the message.</param>
    /// <param name="runId">The run identifier associated with this execution.</param>
    public static void StreamBEmpty(
        this ILogger logger,
        string runId
    )
    {
        StreamBEmptyMessage(logger, runId, null);
    }

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
    )
    {
        ReadCountsABMessage(logger, runId, countA, countB, null);
    }

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
    )
    {
        RequestingDeactivationsMessage(logger, runId, brookKey, null);
    }
}
