using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Validates the minimal runnable onboarding shape for replica sinks.
/// </summary>
internal interface IReplicaSinkStartupValidator
{
    /// <summary>
    ///     Validates the current replica sink registrations and throws when onboarding prerequisites are missing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when validation finishes.</returns>
    ValueTask ValidateAsync(
        CancellationToken cancellationToken
    );
}