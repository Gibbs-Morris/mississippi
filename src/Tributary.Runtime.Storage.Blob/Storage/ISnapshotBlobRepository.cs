using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Provides increment-2 Blob repository primitives for conditional create and stream-local version discovery.
/// </summary>
internal interface ISnapshotBlobRepository
{
    /// <summary>
    ///     Returns the highest listed snapshot version for the given stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest listed version, or <see langword="null" /> when none exist.</returns>
    Task<long?> GetLatestVersionAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Lists snapshot versions for the given stream page by page.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous sequence of parsed version pages.</returns>
    IAsyncEnumerable<IReadOnlyList<long>> ListVersionsAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes the Blob body for a snapshot version when no Blob already exists at the target name.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key being written.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the Blob has been created or fails for a duplicate version.</returns>
    Task WriteIfAbsentAsync(
        SnapshotKey snapshotKey,
        Stream content,
        CancellationToken cancellationToken = default
    );
}