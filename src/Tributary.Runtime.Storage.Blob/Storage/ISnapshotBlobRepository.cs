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
    ///     Deletes a specific snapshot version when it exists.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the delete attempt finishes.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes all snapshots for the supplied stream.
    /// </summary>
    /// <param name="streamKey">The stream to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when all listed snapshots for the stream have been deleted.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

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
    ///     Prunes snapshots for the supplied stream, always retaining the latest version and any version matching a
    ///     non-zero modulus.
    /// </summary>
    /// <param name="streamKey">The stream to prune.</param>
    /// <param name="retainModuli">The modulus values to retain.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when pruning finishes.</returns>
    Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decoded snapshot envelope, or <see langword="null" /> when the Blob does not exist.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the latest snapshot version for the supplied stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest snapshot envelope, or <see langword="null" /> when no snapshots exist.</returns>
    Task<SnapshotEnvelope?> ReadLatestAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Encodes and writes a snapshot version when no Blob already exists at the target name.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key being written.</param>
    /// <param name="snapshot">The snapshot envelope to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the Blob has been created or fails for a duplicate version.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
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