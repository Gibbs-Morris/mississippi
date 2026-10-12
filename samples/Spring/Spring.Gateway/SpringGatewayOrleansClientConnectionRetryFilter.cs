using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Messaging;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Retries the initial Orleans client connection while the Aspire-managed Spring runtime finishes publishing gateways.
/// </summary>
internal sealed class SpringGatewayOrleansClientConnectionRetryFilter : IClientConnectionRetryFilter
{
    private const int MaxConnectionRetries = 180;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private int connectionRetries;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringGatewayOrleansClientConnectionRetryFilter" /> class.
    /// </summary>
    /// <param name="timeProvider">The clock used for retry delays.</param>
    /// <param name="logger">The logger used for connection retry diagnostics.</param>
    public SpringGatewayOrleansClientConnectionRetryFilter(
        TimeProvider timeProvider,
        ILogger<SpringGatewayOrleansClientConnectionRetryFilter> logger
    )
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<SpringGatewayOrleansClientConnectionRetryFilter> Logger { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<bool> ShouldRetryConnectionAttempt(
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        Logger.ConnectionRetryStarted(exception);
        if (cancellationToken.IsCancellationRequested ||
            exception is not (OrleansMessageRejectionException or ConnectionFailedException))
        {
            Logger.ConnectionRetryCompleted(false);
            return false;
        }

        int retry = Interlocked.Increment(ref connectionRetries);
        if (retry > MaxConnectionRetries)
        {
            Logger.ConnectionRetriesExhausted(MaxConnectionRetries);
            Logger.ConnectionRetryCompleted(false);
            return false;
        }

        try
        {
            await Task.Delay(RetryDelay, TimeProvider, cancellationToken);
            bool shouldRetry = !cancellationToken.IsCancellationRequested;
            Logger.ConnectionRetryCompleted(shouldRetry);
            return shouldRetry;
        }
        catch (OperationCanceledException canceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.ConnectionRetryCanceled(canceledException);
            Logger.ConnectionRetryCompleted(false);
            return false;
        }
    }
}