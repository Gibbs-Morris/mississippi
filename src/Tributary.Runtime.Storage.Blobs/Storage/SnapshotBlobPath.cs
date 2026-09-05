using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Builds and parses Blob names for snapshot documents.
/// </summary>
internal static class SnapshotBlobPath
{
    private const string BlobExtension = ".json";

    /// <summary>
    ///     Builds the Blob name for a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <returns>The Blob name.</returns>
    public static string BuildSnapshotBlobName(
        SnapshotKey snapshotKey
    ) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{BuildStreamPrefix(snapshotKey.Stream)}{snapshotKey.Version:D20}{BlobExtension}");

    /// <summary>
    ///     Builds the Blob prefix for a snapshot stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream key.</param>
    /// <returns>The Blob prefix.</returns>
    public static string BuildStreamPrefix(
        SnapshotStreamKey streamKey
    )
    {
        ArgumentNullException.ThrowIfNull(streamKey.BrookName);
        string streamKeyText = streamKey.ToString();
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(streamKeyText));
        string hash = Convert.ToHexString(hashBytes);
        return $"v1/streams/{hash}/versions/";
    }

    /// <summary>
    ///     Attempts to parse a version from a Blob name under the given stream.
    /// </summary>
    /// <param name="blobName">The Blob name.</param>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="version">The parsed version when successful.</param>
    /// <returns><c>true</c> when the Blob name matches the stream path and contains a valid version.</returns>
    public static bool TryParseVersionFromBlobName(
        string blobName,
        SnapshotStreamKey streamKey,
        out long version
    )
    {
        ArgumentNullException.ThrowIfNull(blobName);
        string prefix = BuildStreamPrefix(streamKey);
        if (!blobName.StartsWith(prefix, StringComparison.Ordinal) ||
            !blobName.EndsWith(BlobExtension, StringComparison.Ordinal))
        {
            version = default;
            return false;
        }

        string versionText = blobName[prefix.Length..^BlobExtension.Length];
        if ((versionText.Length != 20) ||
            !long.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out version))
        {
            version = default;
            return false;
        }

        return true;
    }
}