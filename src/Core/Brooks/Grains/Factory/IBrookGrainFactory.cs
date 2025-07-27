using Mississippi.Core.Abstractions.Brooks;
using Mississippi.Core.Brooks.Grains.Head;
using Mississippi.Core.Brooks.Grains.Reader;
using Mississippi.Core.Brooks.Grains.Writer;


namespace Mississippi.Core.Brooks.Grains.Factory;

/// <summary>
///     Defines a factory for resolving Orleans stream grains for writing, reading, slicing, and head retrieval.
/// </summary>
public interface IBrookGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="IBrookWriterGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookWriterGrain" /> instance for the stream.</returns>
    IBrookWriterGrain GetBrookWriterGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookReaderGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookReaderGrain" /> instance for the stream.</returns>
    IBrookReaderGrain GetBrookReaderGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookSliceReaderGrain" /> for the specified stream composite range key.
    /// </summary>
    /// <param name="brookRangeKey">The key and range identifying the stream slice.</param>
    /// <returns>An <see cref="IBrookSliceReaderGrain" /> instance for the stream slice.</returns>
    IBrookSliceReaderGrain GetBrookSliceReaderGrain(
        BrookRangeKey brookRangeKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookHeadGrain" /> for the specified stream composite key.
    /// </summary>
    /// <param name="brookKeypositeKey">The key identifying the stream.</param>
    /// <returns>An <see cref="IBrookHeadGrain" /> instance for the stream head.</returns>
    IBrookHeadGrain GetBrookHeadGrain(
        BrookKey brookKey
    );
}