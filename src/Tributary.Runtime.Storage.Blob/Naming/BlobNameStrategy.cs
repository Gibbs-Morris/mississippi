using System;
using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Naming;

/// <summary>
///     Canonicalizes snapshot stream identity and derives deterministic Blob names.
/// </summary>
internal sealed class BlobNameStrategy : IBlobNameStrategy
{
    private const int VersionDigits = 20;

    private const string BlobSuffix = ".snapshot";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobNameStrategy" /> class.
    /// </summary>
    /// <param name="options">The configured Blob storage options.</param>
    public BlobNameStrategy(
        IOptions<SnapshotBlobStorageOptions> options
    ) =>
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private SnapshotBlobStorageOptions Options { get; }

    /// <inheritdoc />
    public string GetCanonicalStreamIdentity(
        SnapshotStreamKey streamKey
    )
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("brookName", streamKey.BrookName);
            writer.WriteString("snapshotStorageName", streamKey.SnapshotStorageName);
            writer.WriteString("entityId", streamKey.EntityId);
            writer.WriteString("reducersHash", streamKey.ReducersHash);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <inheritdoc />
    public string GetBlobName(
        SnapshotKey snapshotKey
    ) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{GetStreamPrefix(snapshotKey.Stream)}v{snapshotKey.Version:D20}{BlobSuffix}");

    /// <inheritdoc />
    public string GetStreamPrefix(
        SnapshotStreamKey streamKey
    )
    {
        string blobPrefix = NormalizeBlobPrefix(Options.BlobPrefix);
        string canonicalIdentity = GetCanonicalStreamIdentity(streamKey);
        string streamHash = ComputeSha256Hex(canonicalIdentity);
        return string.Concat(blobPrefix, streamHash, "/");
    }

    /// <inheritdoc />
    public bool TryParseVersion(
        string blobName,
        SnapshotStreamKey streamKey,
        out long version
    ) =>
        TryParseVersion(blobName, GetStreamPrefix(streamKey), out version);

    private static string ComputeSha256Hex(
        string value
    )
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(value);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static string NormalizeBlobPrefix(
        string? blobPrefix
    )
    {
        if (string.IsNullOrWhiteSpace(blobPrefix))
        {
            return string.Empty;
        }

        string normalizedPrefix = blobPrefix.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedPrefix) ? string.Empty : string.Concat(normalizedPrefix, "/");
    }

    private static bool TryParseVersion(
        string blobName,
        string streamPrefix,
        out long version
    )
    {
        version = default;

        if (!blobName.StartsWith(streamPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> suffix = blobName.AsSpan(streamPrefix.Length);
        if ((suffix.Length != (VersionDigits + BlobSuffix.Length + 1)) || (suffix[0] != 'v'))
        {
            return false;
        }

        ReadOnlySpan<char> versionSpan = suffix.Slice(1, VersionDigits);
        if (!suffix[(VersionDigits + 1)..].SequenceEqual(BlobSuffix.AsSpan()))
        {
            return false;
        }

        return long.TryParse(versionSpan, NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}