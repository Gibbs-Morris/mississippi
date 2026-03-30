using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Provides testable interception points around latest-state checkpointing.
/// </summary>
internal interface IReplicaSinkLatestStateProcessorHook
{
    /// <summary>
    ///     Runs after a provider has returned a terminal outcome but before the committed checkpoint is persisted.
    /// </summary>
    /// <param name="deliveryIdentity">The affected delivery lane.</param>
    /// <param name="sourcePosition">The source position being checkpointed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous hook invocation.</returns>
    Task AfterProviderWriteBeforeCheckpointAsync(
        ReplicaSinkDeliveryIdentity deliveryIdentity,
        long sourcePosition,
        CancellationToken cancellationToken = default
    );
}
