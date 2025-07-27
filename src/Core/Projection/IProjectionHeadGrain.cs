using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

[Alias("Mississippi.Core.Projection.IProjectionHeadGrain")]
public interface IProjectionHeadGrain<TModel> : IGrainWithStringKey
{
    [ReadOnly]
    [Alias("GetHeadPositionAsync")]
    Task<long> GetHeadPositionAsync();
}