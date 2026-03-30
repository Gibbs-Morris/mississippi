using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Persists durable delivery state for a replica sink delivery lane.
/// </summary>
public interface IReplicaSinkDeliveryStateStore
{
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
