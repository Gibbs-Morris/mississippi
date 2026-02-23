using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Runtime.Reader;


namespace Mississippi.Brooks.Runtime.Factory;

/// <summary>
///     Extended factory interface for resolving internal Orleans grains.
/// </summary>
/// <remarks>
///     This interface extends <see cref="IBrookGrainFactory" /> with methods for internal grain access
///     (e.g., slice readers) that should not be exposed to external consumers.
///     External code should depend on <see cref="IBrookGrainFactory" /> from the abstractions package.
/// </remarks>
internal interface IInternalBrookGrainFactory : IBrookGrainFactory
{
    /// <summary>
    ///     Retrieves an <see cref="IBrookSliceReaderGrain" /> for the specified Brook composite range key.
    /// </summary>
    /// <param name="brookRangeKey">The key and range identifying the Brook slice.</param>
    /// <returns>An <see cref="IBrookSliceReaderGrain" /> instance for the Brook slice.</returns>
    IBrookSliceReaderGrain GetBrookSliceReaderGrain(
        BrookRangeKey brookRangeKey
    );
}