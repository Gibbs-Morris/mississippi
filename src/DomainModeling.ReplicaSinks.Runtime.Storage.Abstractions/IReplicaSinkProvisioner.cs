using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines target validation and provisioning operations for replica sink providers.
/// </summary>
public interface IReplicaSinkProvisioner
{
    /// <summary>
    ///     Validates or provisions the target described by <paramref name="target" />.
    /// </summary>
    /// <param name="target">The target to validate or provision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task that completes when the target is ready for writes.</returns>
    ValueTask EnsureTargetAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    );
}