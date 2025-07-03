using Mississippi.Core.Cqrs.Query.Grains;

using Orleans.Concurrency;


namespace Mississippi.Core.Abstractions.Cqrs.Query;

/// <summary>
///     One grain instance per logical query *and* per event version.
///     Never called directly by clients – only by <see cref="IQueryGrain{TQuery}" />.
/// </summary>
/// <typeparam name="TQuery">The query state type.</typeparam>
[Alias("Mississippi.Core.Abstractions.Cqrs.IVersionedQueryGrain")]
internal interface IVersionedQueryGrain<TQuery> : IGrainWithStringKey
{
    /// <summary>
    ///     Reads the query snapshot at the specified version or the latest if <c>null</c>.
    /// </summary>
    /// <returns>A task that returns the <see cref="QuerySnapshot{TQuery}" />.</returns>
    [Alias("ReadQuerySnapshotAsync")]
    [ReadOnly]
    Task<QuerySnapshot<TQuery>> ReadQuerySnapshotAsync();
}