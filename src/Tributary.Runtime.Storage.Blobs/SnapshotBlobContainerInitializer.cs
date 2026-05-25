using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Ensures the snapshot Blob container exists when the host starts.
/// </summary>
internal sealed class SnapshotBlobContainerInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobContainerInitializer" /> class.
    /// </summary>
    /// <param name="operations">The Blob operations abstraction.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobContainerInitializer(
        ISnapshotBlobOperations operations,
        ILogger<SnapshotBlobContainerInitializer> logger
    )
    {
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<SnapshotBlobContainerInitializer> Logger { get; }

    private ISnapshotBlobOperations Operations { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        Logger.EnsuringSnapshotContainer();
        await Operations.CreateContainerIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        Logger.SnapshotContainerReady();
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}