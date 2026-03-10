using System;
using System.Globalization;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Builds deterministic Blob names and stream prefixes for Tributary snapshots.
/// </summary>
internal static class SnapshotBlobPathBuilder
{
    private const string SnapshotSuffix = ".snapshot";

    /// <summary>
    ///     Builds the Blob name for a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <returns>The deterministic Blob name.</returns>
    public static string BuildBlobName(
        SnapshotKey snapshotKey
    ) =>
        $"{BuildStreamPrefix(snapshotKey.Stream)}{snapshotKey.Version.ToString(CultureInfo.InvariantCulture)}{SnapshotSuffix}";

    /// <summary>
    ///     Builds the Blob prefix for all versions belonging to a snapshot stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <returns>The deterministic Blob prefix.</returns>
    public static string BuildStreamPrefix(
        SnapshotStreamKey streamKey
    ) =>
        $"{Escape(streamKey.BrookName)}/{Escape(streamKey.SnapshotStorageName)}/{Escape(streamKey.EntityId)}/{Escape(streamKey.ReducersHash)}/";

    /// <summary>
    ///     Attempts to parse a snapshot version from a Blob name that belongs to the provided stream.
    /// </summary>
    /// <param name="streamKey">The expected snapshot stream key.</param>
    /// <param name="blobName">The Blob name to inspect.</param>
    /// <param name="version">The parsed version when successful.</param>
    /// <returns><see langword="true" /> when the Blob belongs to the stream and contains a valid version.</returns>
    public static bool TryParseVersion(
        SnapshotStreamKey streamKey,
        string blobName,
        out long version
    )
    {
        string prefix = BuildStreamPrefix(streamKey);
        if (!blobName.StartsWith(prefix, StringComparison.Ordinal) ||
            !blobName.EndsWith(SnapshotSuffix, StringComparison.Ordinal))
        {
            version = default;
            return false;
        }

        ReadOnlySpan<char> versionSpan = blobName.AsSpan(prefix.Length, blobName.Length - prefix.Length - SnapshotSuffix.Length);
        if (versionSpan.Contains('/'))
        {
            version = default;
            return false;
        }

        return long.TryParse(versionSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out version);
    }

    private static string Escape(
        string value
    ) =>
        Uri.EscapeDataString(value);
}
