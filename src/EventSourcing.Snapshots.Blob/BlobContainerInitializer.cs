using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Blob.Storage;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Hosted service that ensures the blob container exists on startup.
/// </summary>
internal sealed class BlobContainerInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobContainerInitializer" /> class.
    /// </summary>
    /// <param name="blobOperations">The blob operations used to ensure container existence.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BlobContainerInitializer(
        IBlobSnapshotOperations blobOperations,
        ILogger<BlobContainerInitializer> logger
    )
    {
        BlobOperations = blobOperations ?? throw new ArgumentNullException(nameof(blobOperations));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IBlobSnapshotOperations BlobOperations { get; }

    private ILogger<BlobContainerInitializer> Logger { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        Logger.InitializingBlobContainer();
        await BlobOperations.EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);
        Logger.BlobContainerInitialized();
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}