using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.Storage;

/// <summary>
///     Abstraction for reading events from persistent storage for a single event stream.
/// </summary>
/// <remarks>
///     Implementations are expected to preserve the order of events and honour the version
///     boundaries supplied via <paramref name="fromVersion" /> and <paramref name="toVersion" />.
/// </remarks>
public interface IStreamReaderService
{
    /// <summary>
    ///     Reads events for the specified stream in ascending version order.
    /// </summary>
    /// <param name="streamId">Stream identifier (see <see cref="Mississippi.Core.Keys.StreamGrainKey" />).</param>
    /// <param name="fromVersion">Inclusive lower bound; <see langword="null" /> to start with the first event.</param>
    /// <param name="toVersion">Inclusive upper bound; <see langword="null" /> to read through the latest event.</param>
    /// <returns>An asynchronous sequence of <see cref="Mississippi.Core.Idea.MississippiEvent" /> items.</returns>
    IAsyncEnumerable<MississippiEvent> ReadStreamAsync(
        StreamGrainKey streamId,
        long? fromVersion = null,
        long? toVersion = null
    );

    /// <summary>
    ///     Gets the most recent version number of the specified stream.
    /// </summary>
    /// <param name="streamId">Stream identifier.</param>
    /// <returns>The highest persisted version number for the stream.</returns>
    Task<long> GetLatestStreamVersionAsync(
        StreamGrainKey streamId
    );
}