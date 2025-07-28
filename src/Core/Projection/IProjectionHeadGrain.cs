using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Grain interface for managing projection head position tracking.
///     Provides read-only access to the current head position of a projection.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[Alias("Mississippi.Core.Projection.IProjectionHeadGrain")]
public interface IProjectionHeadGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current head position of the projection.
    ///     The head position indicates the latest event sequence number that has been processed by the projection.
    /// </summary>
    /// <returns>The head position as a long value.</returns>
    [ReadOnly]
    [Alias("GetHeadPositionAsync")]
    Task<long> GetHeadPositionAsync();
}