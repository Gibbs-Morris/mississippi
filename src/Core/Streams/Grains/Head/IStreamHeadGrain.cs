using Orleans.Concurrency;


namespace Mississippi.Core.Streams.Grains.Head;

/// <summary>
///     Orleans grain contract that exposes the head (latest version) of an event stream.
/// </summary>
/// <remarks>
///     The grain key for implementations is the string produced by
///     <see cref="Mississippi.Core.Keys.StreamGrainKey" />, providing
///     a unique identifier for each event stream.
/// </remarks>
[Alias("Mississippi.Core.IStreamHeadGrain")]
public interface IStreamHeadGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Retrieves the most recent version number of the associated event stream.
    /// </summary>
    /// <returns>The highest persisted version of the stream.</returns>
    [ReadOnly]
    [Alias("GetLatestPositionAsync")]
    Task<long> GetLatestPositionAsync();
}