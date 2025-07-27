using Orleans.Concurrency;

namespace Mississippi.Core.Projection;

// Entry Point // Stateless
[Alias("ProjectionGrain")]
public interface IProjectionGrain<TModel> : IGrainWithStringKey
{
    [Alias("GetAsync")]
    [ReadOnly]
    Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync();

    [ReadOnly]
    [Alias("GetHeadPositionAsync")]
    Task<long> GetHeadPositionAsync();
}