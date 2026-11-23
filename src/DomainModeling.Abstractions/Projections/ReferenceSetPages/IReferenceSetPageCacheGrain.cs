using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;

/// <summary>Serves bounded pages from a version-keyed immutable membership index.</summary>
/// <typeparam name="TProjection">The named membership projection.</typeparam>
/// <typeparam name="TSource">The referenced source projection type.</typeparam>
/// <remarks>
///     Public for Orleans proxy generation. Application consumers use IReferenceSetPageReader instead;
///     the runtime supplies the existing UxProjectionVersionedCacheKey identity.
/// </remarks>
[Alias("Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages.IReferenceSetPageCacheGrain`2")]
public interface IReferenceSetPageCacheGrain<TProjection, TSource> : IGrainWithStringKey
{
    /// <summary>Reads a bounded selection at this activation's fixed collection version.</summary>
    /// <param name="offset">The zero-based nonnegative offset.</param>
    /// <param name="limit">The maximum entries, from 1 through 100.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A page paired with this activation's version and membership count.</returns>
    [ReadOnly]
    [Alias("ReadAsync")]
    ValueTask<ReferenceSetPage<TProjection, TSource>> ReadAsync(
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    );
}