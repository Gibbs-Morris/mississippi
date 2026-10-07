using System;
using System.Threading;

using Microsoft.Extensions.Logging;

using Mississippi.Inlet.Client.ActionEffects;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Starts a competing provider call at the failed-start diagnostic boundary.
/// </summary>
internal sealed class StartupFailureRetryLogger : ILogger<HubConnectionProvider>
{
    private readonly Action retry;

    private int retryStarted;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StartupFailureRetryLogger" /> class.
    /// </summary>
    /// <param name="retry">The competing startup callback.</param>
    public StartupFailureRetryLogger(
        Action retry
    )
    {
        ArgumentNullException.ThrowIfNull(retry);
        this.retry = retry;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(
        TState state
    )
        where TState : notnull =>
        null;

    /// <inheritdoc />
    public bool IsEnabled(
        LogLevel logLevel
    ) =>
        true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if ((eventId.Id == 3) && (Interlocked.Exchange(ref retryStarted, 1) == 0))
        {
            retry();
        }
    }
}