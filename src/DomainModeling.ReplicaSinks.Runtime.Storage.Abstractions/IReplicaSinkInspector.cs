using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines inspection operations for replica sink providers.
/// </summary>
public interface IReplicaSinkInspector
{
    /// <summary>
    ///     Inspects the observable state of the supplied target.
    /// </summary>
    /// <param name="target">The target to inspect.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A snapshot of the target state.</returns>
    ValueTask<ReplicaTargetInspection> InspectAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    );
}