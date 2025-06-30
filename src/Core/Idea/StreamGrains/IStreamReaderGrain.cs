using Orleans.Concurrency;


namespace Mississippi.Core.Idea.StreamGrains;

/// <summary>
///     Orleans grain contract that provides read access to the full event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="Mississippi.Core.Keys.StreamGrainKey" />, ensuring a one-to-one
///     correspondence with the underlying event stream.
/// </remarks>
[Alias("Mississippi.Core.IStreamReaderGrain")]
public interface IStreamReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Streams events from the underlying event stream within the optional version range.
    /// </summary>
    /// <param name="fromVersion">
    ///     Inclusive lower bound of the version range. <see langword="null" /> to start at the first
    ///     event.
    /// </param>
    /// <param name="toVersion">
    ///     Inclusive upper bound of the version range. <see langword="null" /> to read to the latest
    ///     event.
    /// </param>
    /// <returns>
    ///     An <see cref="IAsyncEnumerable{T}" /> that yields <see cref="Mississippi.Core.Idea.MississippiEvent" />
    ///     instances in ascending version order.
    /// </returns>
    [ReadOnly]
    [Alias("ReadStreamSliceAsync")]
    IAsyncEnumerable<MississippiEvent> ReadEventsAsync(
        long? fromVersion = null,
        long? toVersion = null
    );
}