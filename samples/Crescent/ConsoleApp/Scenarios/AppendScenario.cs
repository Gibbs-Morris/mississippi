using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Writer;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs append scenarios against a specific brook.
/// </summary>
internal static class AppendScenario
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
    /// <param name="expectedCursor">Optional expected cursor for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        string scenarioName,
        Func<ImmutableArray<BrookEvent>> eventFactory,
        BrookPosition? expectedCursor = null,
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
            BrookPosition newCursor = await writer.AppendEventsAsync(events, expectedCursor, cancellationToken);
            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            int elapsedMs = (int)elapsed.TotalMilliseconds;
            logger.AppendComplete(
                runId,
                scenarioName,
                newCursor.Value,
                elapsedMs,
                events.Length / Math.Max(0.001, elapsed.TotalSeconds),
                totalBytes / 1_000_000.0 / Math.Max(0.001, elapsed.TotalSeconds));
            return ScenarioResult.Success(
                scenarioName,
                elapsedMs,
                $"Appended {events.Length} events, cursor={newCursor.Value}");
        }
        catch (OperationCanceledException ex)
        {
            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            int elapsedMs = (int)elapsed.TotalMilliseconds;
            logger.AppendFailed(runId, scenarioName, elapsedMs, events.Length, totalBytes, ex);
            return ScenarioResult.Failure(scenarioName, $"Operation cancelled: {ex.Message}", elapsedMs);
        }
        catch (InvalidOperationException ex)
        {
            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            int elapsedMs = (int)elapsed.TotalMilliseconds;
            logger.AppendFailed(runId, scenarioName, elapsedMs, events.Length, totalBytes, ex);
            return ScenarioResult.Failure(scenarioName, ex.Message, elapsedMs);
        }
    }
}