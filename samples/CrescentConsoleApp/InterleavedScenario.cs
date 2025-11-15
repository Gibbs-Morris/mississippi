using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

using Orleans.Runtime;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Runs a single-stream interleaved read/write scenario.
/// </summary>
internal static class InterleavedScenario
{
    /// <summary>
    ///     Executes the interleaved scenario.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook to exercise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        CancellationToken cancellationToken = default
    )
    {
        logger.InterleaveStart(runId);
        IBrookWriterGrain writer = brookGrainFactory.GetBrookWriterGrain(brookKey);
        IBrookReaderGrain reader = brookGrainFactory.GetBrookReaderGrain(brookKey);

        // Write a small batch
        BrookPosition head1 = await writer.AppendEventsAsync(
            SampleEventFactory.CreateFixedSizeEvents(5, 1024),
            null,
            cancellationToken);
        logger.HeadAfterWrite1(runId, head1.Value);

        // Read a tail subset
        int tailCount = 0;
        long tailStart = Math.Max(1, head1.Value - Math.Min(4, head1.Value));
        await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(new(tailStart), head1, cancellationToken))
        {
            tailCount++;
        }

        logger.TailReadCount(runId, tailCount);

        // Write another mixed batch
        BrookPosition head2 = await writer.AppendEventsAsync(
            SampleEventFactory.CreateRangeSizeEvents(20, 512, 4096),
            head1,
            cancellationToken);
        logger.HeadAfterWrite2(runId, head2.Value);

        // Verify continuous read from 1..head2
        int attempts = 0;
        while (true)
        {
            try
            {
                int count = 0;
                await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(new(1), head2, cancellationToken))
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
    }
}