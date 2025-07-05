using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

[Alias("ProjectionGrain")]
public interface IProjectionGrain<TModel> : IGrainWithStringKey
{
    [Alias("GetAsync")]
    [ReadOnly]
    Task<ProjectionSnapshot<TModel>> GetAsync();
}

public interface IProjectionSnapshotGrain<TModel> : IProjectionGrain<TModel>
{
}

public interface IProjectionHeadGrain<TModel> : IGrainWithStringKey
{
    [ReadOnly]
    Task<long> GetHeadPosition();
}

[ImplicitStreamSubscription("", "")]
public class ProjectionHeadGrain<TModel> : IProjectionHeadGrain<TModel>
{
    private readonly long version = 0;

    public Task<long> GetHeadPosition() => Task.FromResult(version);
}