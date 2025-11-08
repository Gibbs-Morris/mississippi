using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Abstract base class for stateless projection grains that provide access to projection data.
///     Handles caching and delegation to persistent snapshot grains for efficient data retrieval.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[StatelessWorker]
public abstract class ProjectionGrain<TModel>
    : Grain,
      IProjectionGrain<TModel>
    where TModel : new()
{
    private Immutable<ProjectionSnapshot<TModel>> CachedSnapshot { get; set; }

    private long HeadPosition { get; set; } = -1;

    /// <summary>
    ///     Gets the current projection snapshot, handling caching and delegation to persistent storage.
    ///     Checks the head position to determine if cached data is still valid.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the current state.</returns>
    public async Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        long position = await head.GetHeadPositionAsync();
        if (position == HeadPosition)
        {
            return CachedSnapshot;
        }

        IPersistantProjectionSnapshotGrain<TModel>? pg =
            GrainFactory.GetGrain<IPersistantProjectionSnapshotGrain<TModel>>(
                this.GetPrimaryKeyString() + "/v" + position);
        Immutable<ProjectionSnapshot<TModel>> data = await pg.GetAsync();
        HeadPosition = position;
        CachedSnapshot = data;
        return data;
    }

    /// <summary>
    ///     Gets the current head position of the projection.
    ///     Delegates to the projection head grain for the actual position value.
    /// </summary>
    /// <returns>The head position as a long value.</returns>
    public async Task<long> GetHeadPositionAsync()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        return await head.GetHeadPositionAsync();
    }
}