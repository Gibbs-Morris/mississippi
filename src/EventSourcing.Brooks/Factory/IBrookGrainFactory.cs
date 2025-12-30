using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.Brooks.Writer;


namespace Mississippi.EventSourcing.Brooks.Factory;

/// <summary>
///     Defines a factory for resolving Orleans grains for writing, reading, slicing, and cursor retrieval.
/// </summary>
public interface IBrookGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="IBrookAsyncReaderGrain" /> for the specified brook.
    ///     Each call returns a unique grain instance with a random suffix in the key, ensuring
    ///     single-use semantics for streaming reads. The grain deactivates itself after use.
    /// </summary>
    /// <param name="brookKey">The key identifying the brook.</param>
    /// <returns>
    ///     An <see cref="IBrookAsyncReaderGrain" /> instance for streaming reads.
    ///     This is a unique instance that will deactivate after the streaming operation completes.
    /// </returns>
    IBrookAsyncReaderGrain GetBrookAsyncReaderGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookCursorGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookCursorGrain" /> instance for the Brook cursor.</returns>
    IBrookCursorGrain GetBrookCursorGrain(
        BrookKey brookKey
    );

    /// <summary>
    ///     Retrieves an <see cref="IBrookReaderGrain" /> for the specified brook.
    ///     This is a stateless worker grain for batch reads. For streaming reads,
    ///     use <see cref="GetBrookAsyncReaderGrain" /> instead.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookReaderGrain" /> instance for batch reads.</returns>
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
    ///     Retrieves an <see cref="IBrookWriterGrain" /> for the specified Brook composite key.
    /// </summary>
    /// <param name="brookKey">The key identifying the Brook.</param>
    /// <returns>An <see cref="IBrookWriterGrain" /> instance for the Brook.</returns>
    IBrookWriterGrain GetBrookWriterGrain(
        BrookKey brookKey
    );
}