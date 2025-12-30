using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;


namespace Crescent.ConsoleApp.Shared;

/// <summary>
///     Helpers for logging readback operations on a brook.
///     Uses <see cref="IBrookAsyncReaderGrain" /> for streaming reads.
/// </summary>
internal static class ReadHelpers
{
    /// <summary>
    ///     Reads and logs events between 1 and the latest cursor (confirmed optionally).
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook to read.</param>
    /// <param name="useConfirmedCursor">If true, uses the confirmed cursor; otherwise latest cursor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LogStreamReadAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey,
        bool useConfirmedCursor = false,
        CancellationToken cancellationToken = default
    )
    {
        BrookPosition latest = useConfirmedCursor
            ? await brookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionConfirmedAsync()
            : await brookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
        logger.ReadbackCursor(runId, latest.Value);
        if (latest.Value < 1)
        {
            logger.NoEventsToRead(runId);
            return;
        }

        int readCount = 0;
        long totalBytes = 0;
        DateTimeOffset started = DateTimeOffset.UtcNow;

        // Use async reader grain for streaming - each call gets a unique instance
        IBrookAsyncReaderGrain reader = brookGrainFactory.GetBrookAsyncReaderGrain(brookKey);
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
                    mississippiEvent.EventType,
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
    }
}