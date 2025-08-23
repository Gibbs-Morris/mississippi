using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Runs a multi-stream interleaved workload and reports heads and counts.
/// </summary>
internal static class MultiStreamScenario
{
    /// <summary>
    ///     Executes the multi-stream scenario and returns the stream states to persist.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <returns>The list of stream states to persist.</returns>
    public static async Task<List<StreamState>> RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory
    )
    {
        BrookKey keyA = new($"test-brook-{Guid.NewGuid():N}", "A");
        BrookKey keyB = new($"test-brook-{Guid.NewGuid():N}", "B");
        IBrookWriterGrain wA = brookGrainFactory.GetBrookWriterGrain(keyA);
        IBrookWriterGrain wB = brookGrainFactory.GetBrookWriterGrain(keyB);
        await wA.AppendEventsAsync(SampleEventFactory.CreateFixedSizeEvents(50, 1024));
        await wB.AppendEventsAsync(SampleEventFactory.CreateRangeSizeEvents(50, 512, 4096));

        // Use confirmed heads to avoid cached -1 during immediate readback
        BrookPosition hA = await brookGrainFactory.GetBrookHeadGrain(keyA).GetLatestPositionConfirmedAsync();
        BrookPosition hB = await brookGrainFactory.GetBrookHeadGrain(keyB).GetLatestPositionConfirmedAsync();
        logger.HeadsAB(runId, hA.Value, hB.Value);

        // Read a portion from each
        IBrookReaderGrain rA = brookGrainFactory.GetBrookReaderGrain(keyA);
        IBrookReaderGrain rB = brookGrainFactory.GetBrookReaderGrain(keyB);
        int ca = 0, cb = 0;
        if (hA.Value >= 1)
        {
            int attemptsA = 0;
            while (true)
            {
                try
                {
                    ca = 0;
                    await foreach (BrookEvent ignoredEvent in rA.ReadEventsAsync(new(1), hA))
                    {
                        ca++;
                    }

                    break;
                }
                catch (EnumerationAbortedException) when (attemptsA == 0)
                {
                    attemptsA++;

                    // retry once
                }
            }
        }
        else
        {
            logger.StreamAEmpty(runId);
        }

        if (hB.Value >= 1)
        {
            int attemptsB = 0;
            while (true)
            {
                try
                {
                    cb = 0;
                    await foreach (BrookEvent ignoredEvent in rB.ReadEventsAsync(new(1), hB))
                    {
                        cb++;
                    }

                    break;
                }
                catch (EnumerationAbortedException) when (attemptsB == 0)
                {
                    attemptsB++;

                    // retry once
                }
            }
        }
        else
        {
            logger.StreamBEmpty(runId);
        }

        logger.ReadCountsAB(runId, ca, cb);
        return new()
        {
            new()
            {
                Type = keyA.Type,
                Id = keyA.Id,
                Head = hA.Value,
            },
            new()
            {
                Type = keyB.Type,
                Id = keyB.Id,
                Head = hB.Value,
            },
        };
    }
}