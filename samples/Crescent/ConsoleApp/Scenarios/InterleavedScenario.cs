using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Shared;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs a single-stream interleaved read/write scenario.
/// </summary>
internal static class InterleavedScenario
{
    private const string ScenarioName = "Interleaved";

    /// <summary>
    ///     Executes the interleaved scenario.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook to exercise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A scenario result indicating success or failure.</returns>
    public static async Task<ScenarioResult> RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        CancellationToken cancellationToken = default
    )
    {
        Stopwatch sw = Stopwatch.StartNew();
        logger.InterleaveStart(runId);
        IBrookWriterGrain writer = brookGrainFactory.GetBrookWriterGrain(brookKey);
        IBrookReaderGrain reader = brookGrainFactory.GetBrookReaderGrain(brookKey);
        try
        {
            // Write a small batch
            BrookPosition cursor1 = await writer.AppendEventsAsync(
                SampleEventFactory.CreateFixedSizeEvents(5, 1024),
                null,
                cancellationToken);
            logger.CursorAfterWrite1(runId, cursor1.Value);

            // Read a tail subset
            int tailCount = 0;
            long tailStart = Math.Max(1, cursor1.Value - Math.Min(4, cursor1.Value));
            await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(
                               new(tailStart),
                               cursor1,
                               cancellationToken))
            {
                tailCount++;
            }

            logger.TailReadCount(runId, tailCount);

            // Write another mixed batch
            BrookPosition cursor2 = await writer.AppendEventsAsync(
                SampleEventFactory.CreateRangeSizeEvents(20, 512, 4096),
                cursor1,
                cancellationToken);
            logger.CursorAfterWrite2(runId, cursor2.Value);

            // Verify continuous read from 1..cursor2
            int attempts = 0;
            while (true)
            {
                try
                {
                    int count = 0;
                    await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(
                                       new(1),
                                       cursor2,
                                       cancellationToken))
                    {
                        count++;
                    }

                    logger.FullRangeReadCount(runId, count);
                    break;
                }
                catch (EnumerationAbortedException ex) when (attempts == 0)
                {
                    attempts++;
                    logger.InterleaveEnumerationAbortedRetry(runId, ex);
                }
            }

            sw.Stop();
            return ScenarioResult.Success(
                ScenarioName,
                (int)sw.ElapsedMilliseconds,
                "Interleaved read/write completed");
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            return ScenarioResult.Failure(
                ScenarioName,
                $"Operation cancelled: {ex.Message}",
                (int)sw.ElapsedMilliseconds);
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            return ScenarioResult.Failure(ScenarioName, ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }
}