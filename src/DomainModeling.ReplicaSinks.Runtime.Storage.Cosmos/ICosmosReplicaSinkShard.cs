using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Defines the per-sink shard surface consumed by the aggregate Cosmos delivery-state store.
/// </summary>
internal interface ICosmosReplicaSinkShard
{
    /// <summary>
    ///     Gets the named sink registration key represented by the shard.
    /// </summary>
    string SinkKey { get; }

    /// <summary>
    ///     Ensures the shard's container exists or is valid according to its configured provisioning mode.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureContainerAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a bounded set of dead-letter states from the shard.
    /// </summary>
    /// <param name="maxCount">The maximum number of states to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The bounded dead-letter states.</returns>
    Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDeadLettersAsync(
        int maxCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a bounded set of due retry states from the shard.
    /// </summary>
    /// <param name="dueAtOrBeforeUtc">The inclusive due-time cutoff.</param>
    /// <param name="maxCount">The maximum number of states to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The bounded due retry states.</returns>
    Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a single durable delivery-state snapshot from the shard.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The persisted durable state, or <see langword="null" /> when no state exists.</returns>
    Task<ReplicaSinkDeliveryState?> ReadStateAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes a single durable delivery-state snapshot to the shard.
    /// </summary>
    /// <param name="state">The durable delivery-state snapshot.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteStateAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    );
}