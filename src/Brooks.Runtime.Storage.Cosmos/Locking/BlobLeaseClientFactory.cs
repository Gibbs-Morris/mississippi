using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

/// <summary>
///     Default factory for creating <see cref="BlobLeaseClient" /> instances.
/// </summary>
internal sealed class BlobLeaseClientFactory : IBlobLeaseClientFactory
{
    /// <inheritdoc />
    public IBlobLeaseClient Create(
        BlobClient blobClient,
        string? leaseId = null
    )
    {
        BlobLeaseClient inner = leaseId is null ? new(blobClient) : new BlobLeaseClient(blobClient, leaseId);
        return new BlobLeaseClientAdapter(inner);
    }
}