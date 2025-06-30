namespace Mississippi.Core.Abstractions.Cqrs.Query;

public class QueryGrain<T> : IQueryGrain<T>
{
    public Task<QuerySnapshot<T>> ReadAsync(
        long? version = null
    ) =>
        throw new NotImplementedException();

    public Task<long> GetCurrentVersionAsync() => throw new NotImplementedException();

    public Task<QueryReference> GetReferenceAsync() => throw new NotImplementedException();

    public Task<VersionedQueryReference> GetVersionedReferenceAsync(
        long version
    ) =>
        throw new NotImplementedException();
}