using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Manages a pool of ripples for list/detail patterns.
///     Handles subscription lifecycle, caching, and batching.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public interface IRipplePool<T> : IAsyncDisposable
    where T : class
{
    /// <summary>
    ///     Gets the current pool statistics.
    /// </summary>
    RipplePoolStats Stats { get; }

    /// <summary>
    ///     Gets or creates a ripple for a specific entity. Returns cached if available.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The ripple instance for the entity.</returns>
    IRipple<T> GetOrCreate(
        string entityId
    );

    /// <summary>
    ///     Marks an entity as no longer visible (demotes to WARM tier).
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    void MarkHidden(
        string entityId
    );

    /// <summary>
    ///     Marks an entity as actively visible (promotes to HOT tier).
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    void MarkVisible(
        string entityId
    );

    /// <summary>
    ///     Prefetch data for entities (batch request, no subscription).
    /// </summary>
    /// <param name="entityIds">The entity identifiers to prefetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PrefetchAsync(
        IEnumerable<string> entityIds,
        CancellationToken cancellationToken = default
    );
}