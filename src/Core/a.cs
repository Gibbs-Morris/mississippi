using Mississippi.Core.Abstractions.Cqrs.Query;
using Mississippi.EventSourcing.Reducer;


namespace Mississippi.Core;

public class BaseQueryGrain<TQuery> : IQueryGrain<TQuery>
{
    public BaseQueryGrain(
        IRootReducer<TQuery> rootReducer,
        IGrainFactory grainFactory
    )
    {
        RootReducer = rootReducer;
        GrainFactory = grainFactory;
    }

    private IGrainFactory GrainFactory { get; }

    private IRootReducer<TQuery> RootReducer { get; }

    private QuerySnapshot<TQuery> Result { get; set; }

    public Task<QuerySnapshot<TQuery>> ReadAsync(
        long? version = null
    )
    {
        if (Result.Reference.Version == version)
        {
            return Task.FromResult(Result);
        }

        IVersionedQueryGrain<TQuery>? versionGrain = GrainFactory.GetGrain<IVersionedQueryGrain<TQuery>>("");
        return versionGrain.ReadQuerySnapshotAsync();
    }

    public Task<long> GetCurrentVersionAsync() => Task.FromResult(Result.Reference.Version);

    public Task<QueryReference> GetReferenceAsync() => Task.FromResult(Result.Reference.ToQueryReference());

    public Task<VersionedQueryReference> GetVersionedReferenceAsync(
        long version
    ) =>
        Task.FromResult(Result.Reference);
}