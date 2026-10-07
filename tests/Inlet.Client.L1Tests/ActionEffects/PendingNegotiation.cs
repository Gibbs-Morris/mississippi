using System.Threading;
using System.Threading.Tasks;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Holds a real SignalR negotiation request until a test releases it.
/// </summary>
internal sealed class PendingNegotiation
{
    private readonly TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Releases the request to the real negotiation endpoint.
    /// </summary>
    internal void Allow() => response.TrySetResult(true);

    /// <summary>
    ///     Completes the request with a temporary server failure.
    /// </summary>
    internal void Reject() => response.TrySetResult(false);

    /// <summary>
    ///     Waits for the test's response decision.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Whether negotiation should proceed.</returns>
    internal Task<bool> WaitAsync(
        CancellationToken cancellationToken
    ) =>
        response.Task.WaitAsync(cancellationToken);
}