using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Cursor;
using Mississippi.EventSourcing.Brooks.Abstractions.Reader;
using Mississippi.EventSourcing.Brooks.Abstractions.Writer;
using Mississippi.EventSourcing.Brooks.Reader;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Factory;

/// <summary>
///     Factory for resolving Orleans grains (writers, readers, slices, and cursor) by key.
/// </summary>
/// <remarks>
///     Implements both the public <see cref="Abstractions.Factory.IBrookGrainFactory" /> and
///     internal <see cref="IInternalBrookGrainFactory" /> interfaces. External consumers should
///     depend on the abstractions interface; internal grains can use the internal interface.
/// </remarks>
internal class BrookGrainFactory : IInternalBrookGrainFactory
{
    private static readonly Action<ILogger, string, BrookAsyncReaderKey, Exception?> LogResolvingAsyncReaderGrain =
        LoggerMessage.Define<string, BrookAsyncReaderKey>(
            LogLevel.Debug,
            new(5, nameof(GetBrookAsyncReaderGrain)),
            "Resolving {GrainType} for Brook {BrookAsyncReaderKey}");

    private static readonly Action<ILogger, string, BrookKey, Exception?> LogResolvingCursorGrain =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Debug,
            new(4, nameof(GetBrookCursorGrain)),
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

    private static readonly Action<ILogger, string, BrookKey, Exception?> LogResolvingWriterGrain =
        LoggerMessage.Define<string, BrookKey>(
            LogLevel.Debug,
            new(1, nameof(GetBrookWriterGrain)),
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
    ///     Retrieves an <see cref="IBrookAsyncReaderGrain" /> for the specified brook.
    ///     Each call returns a unique grain instance with a random suffix in the key.
    /// </summary>
    /// <param name="brookKey">The key identifying the brook.</param>
    /// <returns>An <see cref="IBrookAsyncReaderGrain" /> instance for streaming reads.</returns>
    public IBrookAsyncReaderGrain GetBrookAsyncReaderGrain(
        BrookKey brookKey
    )
    {
        BrookAsyncReaderKey asyncReaderKey = BrookAsyncReaderKey.Create(brookKey);
        LogResolvingAsyncReaderGrain(Logger, nameof(IBrookAsyncReaderGrain), asyncReaderKey, null);
        return GrainFactory.GetGrain<IBrookAsyncReaderGrain>(asyncReaderKey);
    }

    /// <summary>
    ///     Retrieves an <see cref="IBrookCursorGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookCursorGrain" /> instance for the Brook cursor.</returns>
    public IBrookCursorGrain GetBrookCursorGrain(
        BrookKey brookKey
    )
    {
        LogResolvingCursorGrain(Logger, nameof(IBrookCursorGrain), brookKey, null);
        return GrainFactory.GetGrain<IBrookCursorGrain>(brookKey);
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
}