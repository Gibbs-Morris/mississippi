using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Cursor;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Reader;


namespace Crescent.ConsoleApp.Shared;

/// <summary>
///     Cache and deactivation helpers for Orleans grains used by the sample.
/// </summary>
internal static class CacheHelpers
{
    /// <summary>
    ///     Triggers deactivation of known grains for a brook to flush caches.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <param name="brookKey">Target brook whose caches will be flushed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This deactivates the high-level cursor and reader grains. Internal slice reader grains
    ///     will deactivate naturally through Orleans' idle deactivation policy.
    /// </remarks>
    public static async Task FlushCachesAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory,
        BrookKey brookKey
    )
    {
        logger.RequestingDeactivations(runId, brookKey);
        IBrookCursorGrain cursorGrain = brookGrainFactory.GetBrookCursorGrain(brookKey);
        IBrookReaderGrain readerGrain = brookGrainFactory.GetBrookReaderGrain(brookKey);
        await cursorGrain.DeactivateAsync();
        await readerGrain.DeactivateAsync();
    }
}