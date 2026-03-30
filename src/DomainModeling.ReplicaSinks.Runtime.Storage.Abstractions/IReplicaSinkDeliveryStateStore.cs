using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Persists durable delivery state for a replica sink delivery lane.
/// </summary>
public interface IReplicaSinkDeliveryStateStore
{
    /// <summary>
    ///     Reads a bounded page of persisted dead-letter snapshots.
    /// </summary>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <param name="continuationToken">The opaque continuation token from a previous page, when present.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The bounded page of dead-letter snapshots.</returns>
    Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
        int pageSize,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the currently due retry snapshots in due-time order.
    /// </summary>
    /// <param name="dueAtOrBeforeUtc">The inclusive due-time cutoff.</param>
    /// <param name="maxCount">The maximum number of due retry snapshots to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The due retry snapshots in due-time order.</returns>
    Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the durable delivery state for the specified delivery key.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The persisted delivery state, or <see langword="null" /> when none has been stored yet.</returns>
    Task<ReplicaSinkDeliveryState?> ReadAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes the durable delivery state for the specified delivery key.
    /// </summary>
    /// <param name="state">The durable delivery state snapshot to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    );
}