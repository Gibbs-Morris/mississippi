using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.Reservoir.Client;

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
internal sealed class DevToolsInitializationCheckerService : BackgroundService
{
    /// <summary>
    ///     Default delay before checking initialization (5 seconds).
    /// </summary>
    internal static readonly TimeSpan DefaultCheckDelay = TimeSpan.FromSeconds(5);

    private TimeSpan CheckDelay { get; }

    private IHostEnvironment? HostEnvironment { get; }

    private ILogger<DevToolsInitializationCheckerService> Logger { get; }

    private ReservoirDevToolsOptions Options { get; }

    private TimeProvider TimeProvider { get; }

    private DevToolsInitializationTracker Tracker { get; }

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
        Tracker = tracker;
        Logger = logger;
        TimeProvider = timeProvider;
        Options = options.Value;
        HostEnvironment = hostEnvironment;
        CheckDelay = checkDelay;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        // Only check if DevTools is actually enabled
        if (!IsEnabled())
        {
            return;
        }

        try
        {
            await Task.Delay(CheckDelay, TimeProvider, stoppingToken);
            Logger.DevToolsInitializationCheckResult(Tracker.WasInitialized);
            if (Tracker.WasInitialized)
            {
                return;
            }

            if (ShouldThrow())
            {
                Logger.DevToolsNotInitializedError();
                throw new InvalidOperationException(
                    "DevTools is registered and enabled but Initialize() was not called. " +
                    "Add <ReservoirDevToolsInitializerComponent/> to your App.razor or root layout. " +
                    "Set ThrowOnMissingInitializer to false in ReservoirDevToolsOptions to log a warning instead.");
            }

            Logger.DevToolsNotInitialized();
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown - ignore
        }
    }

    private bool IsEnabled()
    {
        return Options.Enablement switch
        {
            ReservoirDevToolsEnablement.Always => true,
            ReservoirDevToolsEnablement.DevelopmentOnly => HostEnvironment?.IsDevelopment() == true,
            var _ => false,
        };
    }

    private bool ShouldThrow()
    {
        // Explicit configuration takes precedence
        if (Options.ThrowOnMissingInitializer.HasValue)
        {
            return Options.ThrowOnMissingInitializer.Value;
        }

        // Default: throw in development, warn in production
        return HostEnvironment?.IsDevelopment() == true;
    }
}