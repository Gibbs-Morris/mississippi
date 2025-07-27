using Mississippi.Core.Abstractions.Streams;
using Orleans.Concurrency;

namespace Mississippi.Core.Brooks.Grains.Head;

/// <summary>
///     Orleans grain contract that exposes the head (latest version) of a Mississippi event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" /> to provide
///     a unique identifier for each Mississippi event stream.
/// </remarks>
[Alias("Mississippi.Core.IBrookHeadGrain")]
public interface IBrookHeadGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Retrieves the most recent version number of the associated event stream.
    /// </summary>
    /// <returns>The highest persisted version of the stream.</returns>
    [ReadOnly]
    [Alias("GetLatestPositionAsync")]
    Task<BrookPosition> GetLatestPositionAsync();
}