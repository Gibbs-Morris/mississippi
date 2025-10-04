using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;


namespace Mississippi.EventSourcing.Factory;

/// <summary>
///     Factory for resolving Orleans grains (writers, readers, slices, and head) by key.
/// </summary>
internal class BrookGrainFactory : IBrookGrainFactory
{
    private static readonly Action<ILogger, string, BrookKey, Exception?> LogResolvingWriterGrain =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Debug,
            new(1, nameof(GetBrookWriterGrain)),
            "Resolving {GrainType} for Brook {BrookKey}");

    private static readonly Action<ILogger, string, BrookKey, Exception?> LogResolvingReaderGrain =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Debug,
            new(2, nameof(GetBrookReaderGrain)),
            "Resolving {GrainType} for Brook {BrookKey}");

    private static readonly Action<ILogger, string, BrookRangeKey, Exception?> LogResolvingSliceReaderGrain =
        LoggerMessage.Define<string, BrookRangeKey>(
            LogLevel.Debug,
            new(3, nameof(GetBrookSliceReaderGrain)),
            "Resolving {GrainType} for Brook {BrookRangeKey}");

    private static readonly Action<ILogger, string, BrookKey, Exception?> LogResolvingHeadGrain =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Debug,
            new(4, nameof(GetBrookHeadGrain)),
            "Resolving {GrainType} for Brook {BrookKey}");

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookGrainFactory" /> class.
    ///     Sets up the factory with Orleans grain factory and logging dependencies.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory for creating grain instances.</param>
    /// <param name="logger">Logger instance for logging grain factory operations.</param>
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
    ///     Retrieves an <see cref="IBrookWriterGrain" /> for the specified brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the brook.</param>
    /// <returns>An <see cref="IBrookWriterGrain" /> instance for the brook.</returns>
    public IBrookWriterGrain GetBrookWriterGrain(
        BrookKey brookKey
    )
    {
        LogResolvingWriterGrain(Logger, nameof(IBrookWriterGrain), brookKey, null);
        return GrainFactory.GetGrain<IBrookWriterGrain>(brookKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookReaderGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookReaderGrain" /> instance for the Brook.</returns>
    public IBrookReaderGrain GetBrookReaderGrain(
        BrookKey brookKey
    )
    {
        LogResolvingReaderGrain(Logger, nameof(IBrookReaderGrain), brookKey, null);
        return GrainFactory.GetGrain<IBrookReaderGrain>(brookKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookSliceReaderGrain" /> for the specified Brook composite range key.
    /// </summary>
    /// <param name="brookRangeKey">The key and range identifying the Brook slice.</param>
    /// <returns>An <see cref="IBrookSliceReaderGrain" /> instance for the Brook slice.</returns>
    public IBrookSliceReaderGrain GetBrookSliceReaderGrain(
        BrookRangeKey brookRangeKey
    )
    {
        LogResolvingSliceReaderGrain(Logger, nameof(IBrookSliceReaderGrain), brookRangeKey, null);
        return GrainFactory.GetGrain<IBrookSliceReaderGrain>(brookRangeKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookHeadGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookHeadGrain" /> instance for the Brook head.</returns>
    public IBrookHeadGrain GetBrookHeadGrain(
        BrookKey brookKey
    )
    {
        LogResolvingHeadGrain(Logger, nameof(IBrookHeadGrain), brookKey, null);
        return GrainFactory.GetGrain<IBrookHeadGrain>(brookKey);
    }
}
