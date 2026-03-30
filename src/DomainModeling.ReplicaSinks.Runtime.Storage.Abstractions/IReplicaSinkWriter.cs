using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines write operations for replica sink providers.
/// </summary>
public interface IReplicaSinkWriter
{
    /// <summary>
    ///     Writes the supplied replica payload to the provider target.
    /// </summary>
    /// <param name="request">The provider-facing write request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The terminal provider write result.</returns>
    ValueTask<ReplicaWriteResult> WriteAsync(
        ReplicaWriteRequest request,
        CancellationToken cancellationToken
    );
}