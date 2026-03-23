using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Naming;

/// <summary>
///     Builds and parses internal Blob names for snapshot streams.
/// </summary>
internal interface IBlobNameStrategy
{
    /// <summary>
    ///     Builds the Blob name for a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to encode.</param>
    /// <returns>The deterministic Blob name.</returns>
    string GetBlobName(
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Gets the canonical persisted stream identity used for hashing and later frame metadata.
    /// </summary>
    /// <param name="streamKey">The logical snapshot stream key.</param>
    /// <returns>The canonical stream identity string.</returns>
    string GetCanonicalStreamIdentity(
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Builds the stream-local Blob prefix for a snapshot stream.
    /// </summary>
    /// <param name="streamKey">The logical snapshot stream key.</param>
    /// <returns>The deterministic stream prefix.</returns>
    string GetStreamPrefix(
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Attempts to parse a snapshot version from a Blob name scoped to the provided stream.
    /// </summary>
    /// <param name="blobName">The Blob name to inspect.</param>
    /// <param name="streamKey">The expected snapshot stream key.</param>
    /// <param name="version">The parsed version when successful.</param>
    /// <returns><see langword="true" /> when the Blob name belongs to the stream and contains a valid version token.</returns>
    bool TryParseVersion(
        string blobName,
        SnapshotStreamKey streamKey,
        out long version
    );
}