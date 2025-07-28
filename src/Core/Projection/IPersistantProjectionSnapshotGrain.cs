using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

[Alias("Mississippi.Core.Projection.IPersistantProjectionSnapshotGrain")]
public interface IPersistantProjectionSnapshotGrain<TModel> : IGrainWithStringKey
{
    // return the snapshot
    // or go to IProjectionSnapshotGeneratorGrain to build it.
    [Alias("GetAsync")]
    [ReadOnly]
    Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync();

    // delete the cache and stored version so a rebuild needs to happen.
    [Alias("InvalidateAsync")]
    [OneWay]
    Task InvalidateAsync();
}

// This is where we build the snapshot
public interface IProjectionSnapshotGeneratorGrain<TModel> : IGrainWithStringKey
{
    [Alias("BuildAsync")]
    Task<Immutable<ProjectionSnapshot<TModel>>> BuildAsync();

    [Alias("BackgroundBuildAsync")]
    Task BackgroundBuildAsync();
}

public abstract class PersistantProjectionSnapshotGrain<TModel>
    : IPersistantProjectionSnapshotGrain<TModel>,
      IGrainBase
{
    private Task<Immutable<ProjectionSnapshot<TModel>>>? _inFlight;

    public required IGrainFactory GrainFactory { get; init; }

    private Immutable<ProjectionSnapshot<TModel>> CachedState { get; set; }

    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        IProjectionSnapshotGeneratorGrain<TModel>? generator =
            GrainFactory.GetGrain<IProjectionSnapshotGeneratorGrain<TModel>>(this.GetPrimaryKeyString());
        await generator.BackgroundBuildAsync();
    }

    public required IGrainContext GrainContext { get; init; }

    public async Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync()
    {
        if (CachedState.Value is not null)
        {
            return CachedState;
        }

        if (_inFlight is null)
        {
            IProjectionSnapshotGeneratorGrain<TModel>? builder =
                GrainFactory.GetGrain<IProjectionSnapshotGeneratorGrain<TModel>>(this.GetPrimaryKeyString());
            _inFlight = builder.BuildAsync();
        }

        CachedState = await _inFlight;
        return CachedState;
    }

    public Task InvalidateAsync()
    {
        CachedState = default;
        return Task.CompletedTask;
    }

    private async Task LoadCachedState()
    {
        // This may seem counter productive not loading it here but IProjectionSnapshotGeneratorGrain does not allow read-only reads so
        // it means we will only ever run the generate once, even if multiple requests come in the same sub second between the request and the first generation to finish.
        IProjectionSnapshotGeneratorGrain<TModel>? generator =
            GrainFactory.GetGrain<IProjectionSnapshotGeneratorGrain<TModel>>(this.GetPrimaryKeyString());
        CachedState = await generator.BuildAsync();
    }
}