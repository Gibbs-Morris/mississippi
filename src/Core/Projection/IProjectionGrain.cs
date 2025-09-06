using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Entry point grain interface for projection operations. This is a stateless grain
///     that serves as the primary access point for projection data and metadata.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[Alias("ProjectionGrain")]
public interface IProjectionGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current projection snapshot with all computed model data.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the current state of the projection.</returns>
    [Alias("GetAsync")]
    [ReadOnly]
    Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync();

    /// <summary>
    ///     Gets the current head position of the projection, indicating the latest processed event position.
    /// </summary>
    /// <returns>The head position as a long value representing the latest processed event sequence number.</returns>
    [ReadOnly]
    [Alias("GetHeadPositionAsync")]
    Task<long> GetHeadPositionAsync();
}