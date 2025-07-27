using Microsoft.Extensions.Logging;

using Mississippi.Core.Abstractions.Brooks;
using Mississippi.Core.Brooks.Grains.Head;
using Mississippi.Core.Brooks.Grains.Reader;
using Mississippi.Core.Brooks.Grains.Writer;


namespace Mississippi.Core.Brooks.Grains.Factory;

/// <summary>
///     Factory for resolving Orleans stream grains (writers, readers, slices, and head) by key.
/// </summary>
internal class BrookGrainFactory : IBrookGrainFactory
{
    public BrookGrainFactory(
        IGrainFactory grainFactory,
        ILogger<BrookGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<BrookGrainFactory> Logger { get; }

    /// <summary>
    ///     Retrieves an <see cref="IBrookWriterGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookWriterGrain" /> instance for the stream.</returns>
    public IBrookWriterGrain GetBrookWriterGrain(
        BrookKey brookKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {BrookKey}", nameof(IBrookWriterGrain), brookKey);
        return GrainFactory.GetGrain<IBrookWriterGrain>(brookKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookReaderGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookReaderGrain" /> instance for the stream.</returns>
    public IBrookReaderGrain GetBrookReaderGrain(
        BrookKey brookKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {BrookKey}", nameof(IBrookReaderGrain), brookKey);
        return GrainFactory.GetGrain<IBrookReaderGrain>(brookKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookSliceReaderGrain" /> for the specified stream composite range key.
    /// </summary>
    /// <param name="brookRangeKey">The key and range identifying the stream slice.</param>
    /// <returns>An <see cref="IBrookSliceReaderGrain" /> instance for the stream slice.</returns>
    public IBrookSliceReaderGrain GetBrookSliceReaderGrain(
        BrookRangeKey brookRangeKey
    )
    {
        Logger.LogDebug(
            "Resolving {GrainType} for stream {BrookRangeKey}",
            nameof(IBrookSliceReaderGrain),
            brookRangeKey);
        return GrainFactory.GetGrain<IBrookSliceReaderGrain>(brookRangeKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookHeadGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookHeadGrain" /> instance for the stream head.</returns>
    public IBrookHeadGrain GetBrookHeadGrain(
        BrookKey brookKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {BrookKey}", nameof(IBrookHeadGrain), brookKey);
        return GrainFactory.GetGrain<IBrookHeadGrain>(brookKey);
    }
}