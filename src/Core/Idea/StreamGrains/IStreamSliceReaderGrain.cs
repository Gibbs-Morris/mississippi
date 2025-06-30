using Orleans.Concurrency;


namespace Mississippi.Core.Idea.StreamGrains;

/// <summary>
///     Orleans grain contract that provides read access to a slice of an event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="Mississippi.Core.Keys.StreamSliceGrainKey" />, representing a
///     specific range within an event stream.
/// </remarks>
[Alias("Mississippi.Core.IStreamSliceReaderGrain")]
public interface IStreamSliceReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Streams the specified slice of events for the current stream.
    /// </summary>
    /// <param name="fromVersion">Inclusive lower bound of the slice. <see langword="null" /> to start at the first event.</param>
    /// <param name="toVersion">Inclusive upper bound of the slice. <see langword="null" /> to end at the latest event.</param>
    /// <returns>
    ///     An <see cref="IAsyncEnumerable{T}" /> containing the requested slice of
    ///     <see cref="Mississippi.Core.Idea.MississippiEvent" /> values.
    /// </returns>
    [ReadOnly]
    [Alias("ReadStreamSliceAsync")]
    IAsyncEnumerable<MississippiEvent> ReadStreamSliceAsync(
        long? fromVersion = null,
        long? toVersion = null
    );
}