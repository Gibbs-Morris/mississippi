namespace Mississippi.Core.Projection;

public abstract class ProjectionGrain<TModel>
    : Grain,
      IProjectionGrain<TModel>
    where TModel : new()
{
    public async Task<ProjectionSnapshot<TModel>> GetAsync()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        long position = await head.GetHeadPosition();
        IProjectionSnapshotGrain<TModel>? pg =
            GrainFactory.GetGrain<IProjectionSnapshotGrain<TModel>>(this.GetPrimaryKeyString() + "/v" + position);
        return await pg.GetAsync();
    }
}

public abstract class ProjectionSnapshotGrain<TModel>
    : Grain,
      IProjectionSnapshotGrain<TModel>
    where TModel : new()
{
    public Task<ProjectionSnapshot<TModel>> GetAsync() =>
        Task.FromResult(
            new ProjectionSnapshot<TModel>
            {
                Data = new(),
                Version = 11,
            });
}