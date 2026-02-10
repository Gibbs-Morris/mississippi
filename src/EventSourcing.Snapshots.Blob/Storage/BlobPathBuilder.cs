using System;
using System.Globalization;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Builds blob paths for snapshot storage.
/// </summary>
internal static class BlobPathBuilder
{
    private const string Extension = ".snapshot";

    /// <summary>
    ///     Builds the blob path for a snapshot.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <returns>The blob path.</returns>
    /// <remarks>
    ///     Path format: {brookName}/{snapshotStorageName}/{entityId}/{reducersHash}/{version}.snapshot.
    /// </remarks>
    public static string BuildPath(
        SnapshotKey snapshotKey
    )
    {
        SnapshotStreamKey stream = snapshotKey.Stream;
        return BuildPath(stream, snapshotKey.Version);
    }

    /// <summary>
    ///     Builds the blob path for a snapshot with an explicit version.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <param name="version">The snapshot version.</param>
    /// <returns>The blob path.</returns>
    public static string BuildPath(
        SnapshotStreamKey streamKey,
        long version
    ) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{EscapeComponent(streamKey.BrookName)}/{EscapeComponent(streamKey.SnapshotStorageName)}/{EscapeComponent(streamKey.EntityId)}/{EscapeComponent(streamKey.ReducersHash)}/{version}{Extension}");

    /// <summary>
    ///     Builds the prefix path for listing all snapshots in a stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <returns>The prefix path for blob listing.</returns>
    public static string BuildPrefix(
        SnapshotStreamKey streamKey
    ) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{EscapeComponent(streamKey.BrookName)}/{EscapeComponent(streamKey.SnapshotStorageName)}/{EscapeComponent(streamKey.EntityId)}/{EscapeComponent(streamKey.ReducersHash)}/");

    /// <summary>
    ///     Extracts the version from a blob path.
    /// </summary>
    /// <param name="blobPath">The blob path.</param>
    /// <returns>The version number, or null if the path is invalid.</returns>
    public static long? ExtractVersion(
        string blobPath
    )
    {
        if (string.IsNullOrEmpty(blobPath))
        {
            return null;
        }

        int lastSlash = blobPath.LastIndexOf('/');
        string fileName = lastSlash >= 0 ? blobPath[(lastSlash + 1)..] : blobPath;
        if (!fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string versionStr = fileName[..^Extension.Length];
        return long.TryParse(versionStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long version)
            ? version
            : null;
    }

    /// <summary>
    ///     Escapes a path component to ensure it's safe for blob storage paths.
    /// </summary>
    /// <param name="component">The component to escape.</param>
    /// <returns>The escaped component.</returns>
    private static string EscapeComponent(
        string component
    ) =>
        Uri.EscapeDataString(component);
}