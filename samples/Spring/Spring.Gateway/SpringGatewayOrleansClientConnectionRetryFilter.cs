using System;
using System.Threading;
using System.Threading.Tasks;

using Orleans;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Retries the initial Orleans client connection while the Aspire-managed Spring runtime finishes publishing gateways.
/// </summary>
internal sealed class SpringGatewayOrleansClientConnectionRetryFilter : IClientConnectionRetryFilter
{
    private const int MaxConnectionAttempts = 60;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private int connectionAttempts;

    /// <inheritdoc />
    public async Task<bool> ShouldRetryConnectionAttempt(
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is null || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        int attempt = Interlocked.Increment(ref connectionAttempts);
        if (attempt > MaxConnectionAttempts)
        {
            return false;
        }

        try
        {
            await Task.Delay(RetryDelay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
