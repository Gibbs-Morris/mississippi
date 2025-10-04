using System.Collections.Immutable;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Writer;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Runs append scenarios against a specific brook.
/// </summary>
internal static class AppendScenarioRunner
{
    /// <summary>
    ///     Runs an append scenario and logs throughput and results.
    /// </summary>
    /// <param name="logger">Logger for structured scenario messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook to append to.</param>
    /// <param name="scenarioName">Friendly scenario name for logging.</param>
    /// <param name="eventFactory">Factory to create the events to append.</param>
    /// <param name="expectedHead">Optional expected head for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    public static async Task RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        string scenarioName,
        Func<ImmutableArray<BrookEvent>> eventFactory,
        BrookPosition? expectedHead = null,
        CancellationToken cancellationToken = default
    )
    {
        IBrookWriterGrain writer = brookGrainFactory.GetBrookWriterGrain(brookKey);
        ImmutableArray<BrookEvent> events = eventFactory();
        long totalBytes = events.Sum(e => (long)e.Data.Length);
        logger.AppendingCounts(runId, scenarioName, events.Length, totalBytes);
        DateTimeOffset started = DateTimeOffset.UtcNow;
        try
        {
            BrookPosition newHead = await writer.AppendEventsAsync(events, expectedHead, cancellationToken);
            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            logger.AppendComplete(
                runId,
                scenarioName,
                newHead.Value,
                (int)elapsed.TotalMilliseconds,
                events.Length / Math.Max(0.001, elapsed.TotalSeconds),
                totalBytes / 1_000_000.0 / Math.Max(0.001, elapsed.TotalSeconds));
        }
        catch (Exception ex)
        {
            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            logger.AppendFailed(runId, scenarioName, (int)elapsed.TotalMilliseconds, events.Length, totalBytes, ex);
            throw;
        }
    }
}
