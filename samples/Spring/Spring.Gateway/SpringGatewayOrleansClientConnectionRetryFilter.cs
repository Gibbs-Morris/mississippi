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
        ArgumentNullException.ThrowIfNull(exception);

        if (Interlocked.Increment(ref connectionAttempts) > MaxConnectionAttempts)
        {
            return false;
        }

        await Task.Delay(RetryDelay, cancellationToken);
        return true;
    }
}
