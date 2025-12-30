using Azure.Storage.Blobs;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     Factory for creating <see cref="Azure.Storage.Blobs.Specialized.BlobLeaseClient" /> instances.
/// </summary>
internal interface IBlobLeaseClientFactory
{
    /// <summary>
    ///     Creates a new <see cref="IBlobLeaseClient" /> for the specified blob.
    /// </summary>
    /// <param name="blobClient">The blob to create a lease client for.</param>
    /// <param name="leaseId">Optional lease id when attaching to an existing lease.</param>
    /// <returns>A new blob lease client adapter.</returns>
    IBlobLeaseClient Create(
        BlobClient blobClient,
        string? leaseId = null
    );
}