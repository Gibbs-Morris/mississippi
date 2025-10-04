using Azure;
using Azure.Storage.Blobs.Models;


namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
///     Abstraction over BlobLeaseClient for testability.
/// </summary>
internal interface IBlobLeaseClient
{
    /// <summary>
    ///     Gets the lease identifier associated with the underlying blob lease.
    /// </summary>
    string LeaseId { get; }

    /// <summary>
    ///     Attempts to acquire a lease on the target blob for the specified duration.
    /// </summary>
    /// <param name="duration">The desired lease duration.</param>
    /// <param name="conditions">Optional request conditions.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A response containing the acquired <see cref="BlobLease" /> on success.</returns>
    Task<Response<BlobLease>> AcquireAsync(
        TimeSpan duration,
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Renews the existing lease associated with the blob.
    /// </summary>
    /// <param name="conditions">Optional request conditions.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A response containing the renewed <see cref="BlobLease" />.</returns>
    Task<Response<BlobLease>> RenewAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Releases the lease associated with the blob.
    /// </summary>
    /// <param name="conditions">Optional request conditions.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A response containing metadata about the release operation.</returns>
    Task<Response<ReleasedObjectInfo>> ReleaseAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    );
}
