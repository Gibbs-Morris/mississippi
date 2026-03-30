using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     In-memory durable-state proof store for Increment 03a latest-state processing.
/// </summary>
public sealed class BootstrapReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
{
    private ConcurrentDictionary<string, ReplicaSinkDeliveryState> States { get; } =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<ReplicaSinkDeliveryState?> ReadAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        cancellationToken.ThrowIfCancellationRequested();
        _ = States.TryGetValue(deliveryKey, out ReplicaSinkDeliveryState? state);
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task WriteAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        States[state.DeliveryKey] = state;
        return Task.CompletedTask;
    }
}
