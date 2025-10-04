using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;


namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
///     Adapter that wraps <see cref="BlobLeaseClient" /> and exposes the <see cref="IBlobLeaseClient" /> abstraction
///     for improved testability.
/// </summary>
internal sealed class BlobLeaseClientAdapter : IBlobLeaseClient
{
    private readonly BlobLeaseClient inner;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobLeaseClientAdapter" /> class.
    /// </summary>
    /// <param name="inner">The underlying <see cref="BlobLeaseClient" /> instance to delegate to.</param>
    public BlobLeaseClientAdapter(
        BlobLeaseClient inner
    ) =>
        this.inner = inner;

    /// <inheritdoc />
    public string LeaseId => inner.LeaseId;

    /// <inheritdoc />
    public Task<Response<BlobLease>> AcquireAsync(
        TimeSpan duration,
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        inner.AcquireAsync(duration, conditions, cancellationToken);

    /// <inheritdoc />
    public Task<Response<BlobLease>> RenewAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        inner.RenewAsync(conditions, cancellationToken);

    /// <inheritdoc />
    public Task<Response<ReleasedObjectInfo>> ReleaseAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        inner.ReleaseAsync(conditions, cancellationToken);
}
