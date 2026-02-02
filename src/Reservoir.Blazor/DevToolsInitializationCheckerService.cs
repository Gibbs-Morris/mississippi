using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Hosted service that checks if DevTools was properly initialized.
/// </summary>
/// <remarks>
///     <para>
///         This service waits for a configurable delay after startup, then checks if
///         <see cref="ReduxDevToolsService.Initialize" /> was called. If not, it logs a warning
///         to help developers diagnose the missing <see cref="ReservoirDevToolsInitializerComponent" />.
///     </para>
///     <para>
///         The check only runs when DevTools enablement is not <see cref="ReservoirDevToolsEnablement.Off" />.
///     </para>
/// </remarks>
internal sealed class DevToolsInitializationCheckerService : IHostedService
{
    /// <summary>
    ///     Default delay before checking initialization (5 seconds).
    /// </summary>
    internal static readonly TimeSpan DefaultCheckDelay = TimeSpan.FromSeconds(5);

    private readonly TimeSpan checkDelay;

    private readonly IHostEnvironment? hostEnvironment;

    private readonly ILogger<DevToolsInitializationCheckerService> logger;

    private readonly ReservoirDevToolsOptions options;

    private readonly TimeProvider timeProvider;

    private readonly DevToolsInitializationTracker tracker;

    private CancellationTokenSource? cts;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DevToolsInitializationCheckerService" /> class.
    /// </summary>
    /// <param name="tracker">The initialization tracker.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for delays.</param>
    /// <param name="options">The DevTools options.</param>
    /// <param name="hostEnvironment">The optional host environment for environment checks.</param>
    public DevToolsInitializationCheckerService(
        DevToolsInitializationTracker tracker,
        ILogger<DevToolsInitializationCheckerService> logger,
        TimeProvider timeProvider,
        IOptions<ReservoirDevToolsOptions> options,
        IHostEnvironment? hostEnvironment = null
    )
        : this(tracker, logger, timeProvider, options, hostEnvironment, DefaultCheckDelay)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DevToolsInitializationCheckerService" /> class
    ///     with a custom check delay (for testing).
    /// </summary>
    /// <param name="tracker">The initialization tracker.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for delays.</param>
    /// <param name="options">The DevTools options.</param>
    /// <param name="hostEnvironment">The optional host environment for environment checks.</param>
    /// <param name="checkDelay">The delay before checking initialization.</param>
    internal DevToolsInitializationCheckerService(
        DevToolsInitializationTracker tracker,
        ILogger<DevToolsInitializationCheckerService> logger,
        TimeProvider timeProvider,
        IOptions<ReservoirDevToolsOptions> options,
        IHostEnvironment? hostEnvironment,
        TimeSpan checkDelay
    )
    {
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        this.tracker = tracker;
        this.logger = logger;
        this.timeProvider = timeProvider;
        this.options = options.Value;
        this.hostEnvironment = hostEnvironment;
        this.checkDelay = checkDelay;
    }

    /// <inheritdoc />
    public Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        // Only check if DevTools is actually enabled
        if (!IsEnabled())
        {
            return Task.CompletedTask;
        }

        // Dispose any previous CTS before creating new one (shouldn't happen but safe)
        cts?.Dispose();
        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Fire and forget - we don't block startup
        _ = CheckInitializationAsync(cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(
        CancellationToken cancellationToken
    )
    {
        if (cts is not null)
        {
            await cts.CancelAsync();
            cts.Dispose();
            cts = null;
        }
    }

    private async Task CheckInitializationAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(checkDelay, timeProvider, cancellationToken);

            logger.DevToolsInitializationCheckResult(tracker.WasInitialized);

            if (!tracker.WasInitialized)
            {
                if (ShouldThrow())
                {
                    logger.DevToolsNotInitializedError();
                    throw new InvalidOperationException(
                        "DevTools is registered and enabled but Initialize() was not called. " +
                        "Add <ReservoirDevToolsInitializerComponent/> to your App.razor or root layout. " +
                        "Set ThrowOnMissingInitializer to false in ReservoirDevToolsOptions to log a warning instead."
                    );
                }

                logger.DevToolsNotInitialized();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown - ignore
        }
    }

    private bool IsEnabled()
    {
        return options.Enablement switch
        {
            ReservoirDevToolsEnablement.Always => true,
            ReservoirDevToolsEnablement.DevelopmentOnly => hostEnvironment?.IsDevelopment() == true,
            var _ => false,
        };
    }

    private bool ShouldThrow()
    {
        // Explicit configuration takes precedence
        if (options.ThrowOnMissingInitializer.HasValue)
        {
            return options.ThrowOnMissingInitializer.Value;
        }

        // Default: throw in development, warn in production
        return hostEnvironment?.IsDevelopment() == true;
    }
}
