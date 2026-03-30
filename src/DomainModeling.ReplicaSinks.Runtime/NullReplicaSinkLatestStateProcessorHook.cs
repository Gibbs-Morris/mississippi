using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     No-op latest-state processor hook used in production composition.
/// </summary>
internal sealed class NullReplicaSinkLatestStateProcessorHook : IReplicaSinkLatestStateProcessorHook
{
    /// <inheritdoc />
    public Task AfterProviderWriteBeforeCheckpointAsync(
        ReplicaSinkDeliveryIdentity deliveryIdentity,
        long sourcePosition,
        CancellationToken cancellationToken = default
    )
    {
        _ = deliveryIdentity;
        _ = sourcePosition;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}