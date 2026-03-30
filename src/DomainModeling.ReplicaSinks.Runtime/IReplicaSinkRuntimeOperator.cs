using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Exposes the bounded runtime/admin operator surface for replica sink dead-letter inspection and controlled re-drive.
/// </summary>
public interface IReplicaSinkRuntimeOperator
{
    /// <summary>
    ///     Reads a bounded page of dead-letter records.
    /// </summary>
    /// <param name="query">The bounded page request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The bounded dead-letter page.</returns>
    Task<ReplicaSinkDeadLetterPage> ReadDeadLettersAsync(
        ReplicaSinkDeadLetterQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Issues a controlled re-drive for a dead-lettered lane by re-reading current eligible source state.
    /// </summary>
    /// <param name="request">The re-drive request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The re-drive outcome.</returns>
    Task<ReplicaSinkDeadLetterReDriveResult> ReDriveAsync(
        ReplicaSinkDeadLetterReDriveRequest request,
        CancellationToken cancellationToken = default
    );
}
