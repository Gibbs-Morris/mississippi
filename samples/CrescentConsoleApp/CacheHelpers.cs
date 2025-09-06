using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;


namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Cache and deactivation helpers for Orleans grains used by the sample.
/// </summary>
internal static class CacheHelpers
{
    /// <summary>
    ///     Triggers deactivation of known grains and slice readers for a brook to flush caches.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook whose caches will be flushed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task FlushCachesAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey
    )
    {
        logger.RequestingDeactivations(runId, brookKey);
        await brookGrainFactory.GetBrookHeadGrain(brookKey).DeactivateAsync();
        await brookGrainFactory.GetBrookReaderGrain(brookKey).DeactivateAsync();

        // Also trigger slice grains to deactivate: touch a few ranges and request deactivation
        BrookPosition head = await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionAsync();
        if (head.Value > 0)
        {
            long step = Math.Max(1, head.Value / 3);
            for (long start = 1; start <= head.Value; start += step)
            {
                long end = Math.Min(head.Value, (start + step) - 1);
                IBrookSliceReaderGrain slice = brookGrainFactory.GetBrookSliceReaderGrain(
                    BrookRangeKey.FromBrookCompositeKey(brookKey, start, end - start));
                await slice.DeactivateAsync();
            }
        }
    }
}