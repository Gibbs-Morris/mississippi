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

/// <summary>
///     Grain interface responsible for building projection snapshots.
///     Handles the computation-intensive task of generating snapshots from source data.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
public interface IProjectionSnapshotGeneratorGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Builds a projection snapshot synchronously and returns the result.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the computed model data.</returns>
    [Alias("BuildAsync")]
    Task<Immutable<ProjectionSnapshot<TModel>>> BuildAsync();

    /// <summary>
    ///     Initiates a background build operation for the projection snapshot.
    ///     This operation does not return the snapshot data immediately.
    /// </summary>
    /// <returns>A task representing the asynchronous background build operation.</returns>
    [Alias("BackgroundBuildAsync")]
    Task BackgroundBuildAsync();
}

/// <summary>
///     Abstract base class for persistent projection snapshot grains.
///     Provides common functionality for managing cached snapshots with in-flight request handling.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
public abstract class PersistantProjectionSnapshotGrain<TModel>
    : IPersistantProjectionSnapshotGrain<TModel>,
      IGrainBase
{
    private Task<Immutable<ProjectionSnapshot<TModel>>>? inFlightTask;

    /// <summary>
    ///     Gets or initializes the Orleans grain factory used for creating other grains.
    ///     Required dependency for accessing other grains in the Orleans cluster.
    /// </summary>
    /// <value>The grain factory instance.</value>
    public required IGrainFactory GrainFactory { get; init; }

    private Immutable<ProjectionSnapshot<TModel>> CachedState { get; set; }

    /// <summary>
    ///     Called when the grain is activated. Initiates background building of the projection snapshot.
    ///     This method is automatically invoked by Orleans when the grain becomes active.
    /// </summary>
    /// <param name="token">Cancellation token for the activation operation.</param>
    /// <returns>A task representing the asynchronous activation operation.</returns>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        IProjectionSnapshotGeneratorGrain<TModel>? generator =
            GrainFactory.GetGrain<IProjectionSnapshotGeneratorGrain<TModel>>(this.GetPrimaryKeyString());
        await generator.BackgroundBuildAsync();
    }

    /// <summary>
    ///     Gets or initializes the Orleans grain context for this grain instance.
    ///     Required Orleans infrastructure dependency for grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public required IGrainContext GrainContext { get; init; }

    /// <summary>
    ///     Gets the projection snapshot, either from cache or by building a new one.
    ///     Implements caching and in-flight request deduplication to avoid rebuilding the same snapshot multiple times.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the current projection state.</returns>
    public async Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync()
    {
        if (CachedState.Value is not null)
        {
            return CachedState;
        }

        if (inFlightTask is null)
        {
            IProjectionSnapshotGeneratorGrain<TModel>? builder =
                GrainFactory.GetGrain<IProjectionSnapshotGeneratorGrain<TModel>>(this.GetPrimaryKeyString());
            inFlightTask = builder.BuildAsync();
        }

        CachedState = await inFlightTask;
        return CachedState;
    }

    /// <summary>
    ///     Invalidates the cached snapshot, forcing the next GetAsync call to rebuild the projection.
    ///     This method clears the cached state but does not rebuild the projection immediately.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task InvalidateAsync()
    {
        CachedState = default;
        return Task.CompletedTask;
    }
}