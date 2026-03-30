using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

internal sealed class ReplicaSinkRuntimeExecutionService : BackgroundService
{
    public ReplicaSinkRuntimeExecutionService(
        IReplicaSinkRuntimeCoordinator coordinator,
        IOptions<ReplicaSinkRuntimeOptions> runtimeOptions,
        TimeProvider timeProvider,
        ILogger<ReplicaSinkRuntimeExecutionService> logger
    )
    {
        Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        RuntimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IReplicaSinkRuntimeCoordinator Coordinator { get; }

    private ILogger<ReplicaSinkRuntimeExecutionService> Logger { get; }

    private IOptions<ReplicaSinkRuntimeOptions> RuntimeOptions { get; }

    private TimeProvider TimeProvider { get; }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(
            Math.Max(1, RuntimeOptions.Value.ExecutionPollInterval.TotalMilliseconds)), TimeProvider);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _ = await Coordinator.ExecuteBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                Logger.ExecutionPumpIterationFailed(ex.GetType().Name);
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private static bool IsCriticalException(
        Exception exception
    ) =>
        exception is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;
}

internal static partial class ReplicaSinkRuntimeExecutionServiceLoggerExtensions
{
    [LoggerMessage(
        EventId = 30,
        Level = LogLevel.Warning,
        Message = "Replica sink runtime execution pump iteration failed with exception type '{ExceptionType}'.")]
    public static partial void ExecutionPumpIterationFailed(
        this ILogger logger,
        string exceptionType
    );
}
