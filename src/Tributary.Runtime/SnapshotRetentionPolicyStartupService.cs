using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     Logs the effective snapshot retention configuration once when the host starts.
/// </summary>
internal sealed class SnapshotRetentionPolicyStartupService : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotRetentionPolicyStartupService" /> class.
    /// </summary>
    /// <param name="options">The configured snapshot retention options.</param>
    /// <param name="logger">The startup policy logger.</param>
    public SnapshotRetentionPolicyStartupService(
        IOptions<SnapshotRetentionOptions> options,
        ILogger<SnapshotRetentionPolicyStartupService> logger
    )
    {
        Options = options;
        Logger = logger;
    }

    private ILogger<SnapshotRetentionPolicyStartupService> Logger { get; }

    private IOptions<SnapshotRetentionOptions> Options { get; }

    /// <inheritdoc />
    public Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        SnapshotRetentionOptions options = Options.Value;
        Logger.PolicyConfigured(
            options.DefaultRetainModulus,
            options.ShouldPersistAllSnapshots,
            options.StateTypeOverrides.Count);
        if (options.ShouldPersistAllSnapshots)
        {
            Logger.PersistAllSnapshotsEnabled();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}