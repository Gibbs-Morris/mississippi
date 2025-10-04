using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Grain interface for managing persistent projection snapshots with caching capabilities.
///     Provides snapshot retrieval and cache invalidation operations.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[Alias("Mississippi.Core.Projection.IPersistantProjectionSnapshotGrain")]
public interface IPersistantProjectionSnapshotGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the projection snapshot, either from cache or by generating a new one.
    ///     If no cached snapshot exists, delegates to IProjectionSnapshotGeneratorGrain to build it.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the model data.</returns>
    [Alias("GetAsync")]
    [ReadOnly]
    Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync();

    /// <summary>
    ///     Invalidates the cached projection snapshot, forcing the next request to rebuild.
    ///     This operation is fire-and-forget and does not return a response.
    /// </summary>
    /// <returns>A task representing the asynchronous invalidation operation.</returns>
    [Alias("InvalidateAsync")]
    [OneWay]
    Task InvalidateAsync();
}
