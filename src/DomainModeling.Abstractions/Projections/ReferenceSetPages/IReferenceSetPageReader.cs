using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;

/// <summary>Reads bounded membership pages for an explicitly registered named projection.</summary>
/// <typeparam name="TProjection">The named membership projection.</typeparam>
/// <typeparam name="TSource">The referenced source projection type.</typeparam>
/// <remarks>Public for typed application consumers. References do not grant permission to load sources.</remarks>
public interface IReferenceSetPageReader<TProjection, TSource>
    where TProjection : class
{
    /// <summary>Reads a page at one captured cursor position or a supplied historical position.</summary>
    /// <param name="entityId">The collection entity ID in the existing application authorization boundary.</param>
    /// <param name="offset">The zero-based offset, from zero through Int32.MaxValue.</param>
    /// <param name="limit">The maximum entries, from 1 through 100; omitted means 50.</param>
    /// <param name="version">The collection version, or null to capture the cached cursor once.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The selection, total count, version and references from the same immutable snapshot.</returns>
    /// <remarks>
    ///     Invalid selections throw ArgumentOutOfRangeException. Pinned reads do not consult the cached cursor.
    ///     Position -1 represents empty history. Unavailable/future snapshot reconstruction and storage failures propagate.
    /// </remarks>
    ValueTask<ReferenceSetPage<TProjection, TSource>> ReadAsync(
        string entityId,
        int offset = 0,
        int limit = 50,
        BrookPosition? version = null,
        CancellationToken cancellationToken = default
    );
}