using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     Adapter that wraps <see cref="BlobLeaseClient" /> and exposes the <see cref="IBlobLeaseClient" /> abstraction
///     for improved testability.
/// </summary>
internal sealed class BlobLeaseClientAdapter : IBlobLeaseClient
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobLeaseClientAdapter" /> class.
    /// </summary>
    /// <param name="inner">The underlying <see cref="BlobLeaseClient" /> instance to delegate to.</param>
    public BlobLeaseClientAdapter(
        BlobLeaseClient inner
    ) =>
        Inner = inner;

    /// <inheritdoc />
    public string LeaseId => Inner.LeaseId;

    private BlobLeaseClient Inner { get; }

    /// <inheritdoc />
    public Task<Response<BlobLease>> AcquireAsync(
        TimeSpan duration,
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        Inner.AcquireAsync(duration, conditions, cancellationToken);

    /// <inheritdoc />
    public Task<Response<ReleasedObjectInfo>> ReleaseAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        Inner.ReleaseAsync(conditions, cancellationToken);

    /// <inheritdoc />
    public Task<Response<BlobLease>> RenewAsync(
        RequestConditions? conditions = null,
        CancellationToken cancellationToken = default
    ) =>
        Inner.RenewAsync(conditions, cancellationToken);
}