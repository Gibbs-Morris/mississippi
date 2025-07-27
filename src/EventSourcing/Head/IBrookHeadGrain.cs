using Mississippi.EventSourcing.Abstractions;

using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Head;

/// <summary>
///     Orleans grain contract that exposes the head (latest version) of a Mississippi brook.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" /> to provide
///     a unique identifier for each Mississippi brook.
/// </remarks>
[Alias("Mississippi.Core.IBrookHeadGrain")]
public interface IBrookHeadGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Retrieves the most recent version number of the associated brook.
    /// </summary>
    /// <returns>The highest persisted version of the brook.</returns>
    [ReadOnly]
    [Alias("GetLatestPositionAsync")]
    Task<BrookPosition> GetLatestPositionAsync();
}