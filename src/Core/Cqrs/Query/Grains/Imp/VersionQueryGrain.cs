namespace Mississippi.Core.Abstractions.Cqrs.Query;

public class VersionQueryGrain<TType> : IVersionedQueryGrain<TType>
{
    public Task<QuerySnapshot<TType>> ReadQuerySnapshotAsync()
    {
        throw new NotImplementedException();
    }
}