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


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs a single-stream interleaved read/write scenario.
///     Uses <see cref="IBrookAsyncReaderGrain" /> for streaming reads, which provides
///     unique grain instances per call to avoid the StatelessWorker + IAsyncEnumerable issue.
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
        logger.InterleaveGrainsResolved(runId, brookKey);
        try
        {
            // Write a small batch
            BrookPosition cursor1 = await writer.AppendEventsAsync(
                SampleEventFactory.CreateFixedSizeEvents(5, 1024),
                null,
                cancellationToken);
            logger.CursorAfterWrite1(runId, cursor1.Value);

            // Read a tail subset using the async reader grain (unique instance per call)
            int tailCount = 0;
            long tailStart = Math.Max(1, cursor1.Value - Math.Min(4, cursor1.Value));
            IBrookAsyncReaderGrain tailReader = brookGrainFactory.GetBrookAsyncReaderGrain(brookKey);
            await foreach (BrookEvent ev in tailReader.ReadEventsAsync(new(tailStart), cursor1, cancellationToken))
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

            // Verify continuous read from 1..cursor2 using async reader grain
            int count = 0;
            logger.InterleaveFullRangeReadStart(runId, 0, 1, cursor2.Value);
            IBrookAsyncReaderGrain fullRangeReader = brookGrainFactory.GetBrookAsyncReaderGrain(brookKey);
            await foreach (BrookEvent ev in fullRangeReader.ReadEventsAsync(new(1), cursor2, cancellationToken))
            {
                count++;
                if ((count <= 5) || ((count % 50) == 0))
                {
                    logger.InterleaveReadProgress(runId, count, ev.Id);
                }
            }

            logger.FullRangeReadCount(runId, count);
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