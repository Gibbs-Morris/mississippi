using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Default implementation of startup Blob container operations.
/// </summary>
internal sealed class BlobContainerInitializerOperations : IBlobContainerInitializerOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobContainerInitializerOperations" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The configured Blob container client.</param>
    public BlobContainerInitializerOperations(
        [FromKeyedServices(SnapshotBlobDefaults.BlobContainerServiceKey)] BlobContainerClient blobContainerClient
    ) =>
        BlobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

    private BlobContainerClient BlobContainerClient { get; }

    /// <inheritdoc />
    public async Task CreateIfNotExistsAsync(
        CancellationToken cancellationToken
    ) =>
        await BlobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        CancellationToken cancellationToken
    ) =>
        await BlobContainerClient.ExistsAsync(cancellationToken);
}