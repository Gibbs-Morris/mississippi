using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Provides increment-2 repository behavior on top of Blob naming and low-level operations.
/// </summary>
internal sealed class SnapshotBlobRepository : ISnapshotBlobRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobRepository" /> class.
    /// </summary>
    /// <param name="blobNameStrategy">The Blob naming strategy.</param>
    /// <param name="blobOperations">The low-level Blob operations seam.</param>
    /// <param name="options">The configured Blob storage options.</param>
    public SnapshotBlobRepository(
        IBlobNameStrategy blobNameStrategy,
        ISnapshotBlobOperations blobOperations,
        IOptions<SnapshotBlobStorageOptions> options
    )
    {
        BlobNameStrategy = blobNameStrategy ?? throw new ArgumentNullException(nameof(blobNameStrategy));
        BlobOperations = blobOperations ?? throw new ArgumentNullException(nameof(blobOperations));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private IBlobNameStrategy BlobNameStrategy { get; }

    private ISnapshotBlobOperations BlobOperations { get; }

    private SnapshotBlobStorageOptions Options { get; }

    /// <inheritdoc />
    public async Task<long?> GetLatestVersionAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        long? latestVersion = null;

        await foreach (IReadOnlyList<long> page in ListVersionsAsync(streamKey, cancellationToken))
        {
            foreach (long version in page)
            {
                latestVersion = !latestVersion.HasValue || (version > latestVersion.Value) ? version : latestVersion;
            }
        }

        return latestVersion;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyList<long>> ListVersionsAsync(
        SnapshotStreamKey streamKey,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        string streamPrefix = BlobNameStrategy.GetStreamPrefix(streamKey);

        await foreach (SnapshotBlobPage page in BlobOperations.ListByPrefixAsync(
                           streamPrefix,
                           Options.ListPageSizeHint,
                           cancellationToken))
        {
            yield return page.BlobNames
                .Select(blobName => BlobNameStrategy.TryParseVersion(blobName, streamKey, out long version)
                    ? (long?)version
                    : null)
                .Where(version => version.HasValue)
                .Select(version => version!.Value)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public async Task WriteIfAbsentAsync(
        SnapshotKey snapshotKey,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        string blobName = BlobNameStrategy.GetBlobName(snapshotKey);
        bool created = await BlobOperations.CreateIfAbsentAsync(blobName, content, cancellationToken).ConfigureAwait(false);
        if (!created)
        {
            throw new SnapshotBlobDuplicateVersionException(snapshotKey);
        }
    }
}