using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;

using Orleans.Runtime;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Helpers for logging readback operations on a brook.
/// </summary>
internal static class ReadHelpers
{
    /// <summary>
    ///     Reads and logs events between 1 and the latest head (confirmed optionally).
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook to read.</param>
    /// <param name="confirmedHead">If true, uses the confirmed head; otherwise latest head.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LogStreamReadAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        bool confirmedHead = false,
        CancellationToken cancellationToken = default
    )
    {
        IBrookReaderGrain reader = brookGrainFactory.GetBrookReaderGrain(brookKey);
        BrookPosition latest = confirmedHead
            ? await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionConfirmedAsync()
            : await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionAsync();
        logger.ReadbackHead(runId, latest.Value);
        if (latest.Value < 1)
        {
            logger.NoEventsToRead(runId);
            return;
        }

        int attempt = 0;
        while (true)
        {
            int readCount = 0;
            long totalBytes = 0;
            DateTimeOffset started = DateTimeOffset.UtcNow;
            try
            {
                await foreach (BrookEvent mississippiEvent in reader.ReadEventsAsync(new(1), latest, cancellationToken))
                {
                    readCount++;
                    totalBytes += mississippiEvent.Data.Length;
                    if ((readCount <= 5) || ((readCount % 50) == 0))
                    {
                        logger.ReadIdxEvent(
                            runId,
                            readCount,
                            mississippiEvent.Id,
                            mississippiEvent.Type,
                            mississippiEvent.Data.Length);
                    }
                }

                TimeSpan elapsed = DateTimeOffset.UtcNow - started;
                logger.ReadbackComplete(
                    runId,
                    readCount,
                    totalBytes,
                    (int)elapsed.TotalMilliseconds,
                    readCount / Math.Max(0.001, elapsed.TotalSeconds),
                    totalBytes / 1_000_000.0 / Math.Max(0.001, elapsed.TotalSeconds));
                break;
            }
            catch (EnumerationAbortedException ex) when (attempt == 0)
            {
                attempt++;
                logger.ReadEnumerationAbortedRetry(runId, ex);

                // loop and retry full read once
            }
        }
    }
}