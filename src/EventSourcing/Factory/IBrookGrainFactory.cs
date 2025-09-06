using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;


namespace Mississippi.EventSourcing.Factory;

/// <summary>
///     Defines a factory for resolving Orleans grains for writing, reading, slicing, and head retrieval.
/// </summary>
public interface IBrookGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="IBrookWriterGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookWriterGrain" /> instance for the Brook.</returns>
    IBrookWriterGrain GetBrookWriterGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookReaderGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookReaderGrain" /> instance for the Brook.</returns>
    IBrookReaderGrain GetBrookReaderGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookSliceReaderGrain" /> for the specified Brook composite range key.
    /// </summary>
    /// <param name="brookRangeKey">The key and range identifying the Brook slice.</param>
    /// <returns>An <see cref="IBrookSliceReaderGrain" /> instance for the Brook slice.</returns>
    IBrookSliceReaderGrain GetBrookSliceReaderGrain(
        BrookRangeKey brookRangeKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookHeadGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookHeadGrain" /> instance for the Brook head.</returns>
    IBrookHeadGrain GetBrookHeadGrain(
        BrookKey brookKey
    );
}