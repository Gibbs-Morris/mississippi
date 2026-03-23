using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;
using Mississippi.Tributary.Runtime.Storage.Blob.Startup;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Encodes and decodes the provider-owned Blob snapshot frame.
/// </summary>
internal sealed class BlobEnvelopeCodec : IBlobEnvelopeCodec
{
    private const int CurrentStorageFormatVersion = 1;

    private const ushort SupportedFrameVersion = 1;

    private const ushort SupportedFlags = 0;

    private const int PreludeLength = 16;

    private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("TRIBSNAP");

    private static readonly JsonSerializerOptions HeaderSerializerOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobEnvelopeCodec" /> class.
    /// </summary>
    /// <param name="blobNameStrategy">The Blob naming strategy.</param>
    /// <param name="payloadSerializerResolver">The payload serializer resolver.</param>
    /// <param name="serializationProviders">The registered serialization providers.</param>
    /// <param name="options">The configured Blob storage options.</param>
    /// <param name="timeProvider">The time provider used for the stored write timestamp.</param>
    public BlobEnvelopeCodec(
        IBlobNameStrategy blobNameStrategy,
        SnapshotPayloadSerializerResolver payloadSerializerResolver,
        IEnumerable<ISerializationProvider> serializationProviders,
        IOptions<SnapshotBlobStorageOptions> options,
        TimeProvider timeProvider
    )
    {
        BlobNameStrategy = blobNameStrategy ?? throw new ArgumentNullException(nameof(blobNameStrategy));
        PayloadSerializerResolver = payloadSerializerResolver ?? throw new ArgumentNullException(nameof(payloadSerializerResolver));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        KnownPayloadSerializerIds = serializationProviders?
            .Select(SnapshotPayloadSerializerResolver.GetSerializerId)
            .ToHashSet(StringComparer.Ordinal)
            ?? throw new ArgumentNullException(nameof(serializationProviders));
    }

    private IBlobNameStrategy BlobNameStrategy { get; }

    private HashSet<string> KnownPayloadSerializerIds { get; }

    private SnapshotBlobStorageOptions Options { get; }

    private SnapshotPayloadSerializerResolver PayloadSerializerResolver { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public DecodedSnapshotBlobFrame Decode(
        SnapshotKey snapshotKey,
        ReadOnlyMemory<byte> storedFrame
    )
    {
        ReadOnlySpan<byte> frameSpan = storedFrame.Span;
        if (frameSpan.Length < PreludeLength)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.TruncatedPrelude,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the fixed prelude is truncated.");
        }

        if (!frameSpan[..MagicBytes.Length].SequenceEqual(MagicBytes))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidMagic,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the frame magic is invalid.");
        }

        ushort frameVersion = BinaryPrimitives.ReadUInt16LittleEndian(frameSpan.Slice(8, sizeof(ushort)));
        if (frameVersion != SupportedFrameVersion)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.UnsupportedFrameVersion,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because frame version '{frameVersion}' is not supported.");
        }

        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(frameSpan.Slice(10, sizeof(ushort)));
        if (flags != SupportedFlags)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.UnsupportedFlags,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because flags value '{flags}' is not supported.");
        }

        int remainingFrameLength = frameSpan.Length - PreludeLength;
        uint rawHeaderLength = BinaryPrimitives.ReadUInt32LittleEndian(frameSpan.Slice(12, sizeof(uint)));
        if ((rawHeaderLength == 0)
            || (rawHeaderLength > int.MaxValue)
            || (rawHeaderLength > Options.MaxHeaderBytes)
            || (rawHeaderLength > remainingFrameLength))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidHeaderLength,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because header length '{rawHeaderLength}' is invalid or exceeds the configured limit of '{Options.MaxHeaderBytes}' bytes.");
        }

        int headerLength = (int)rawHeaderLength;

        StoredSnapshotBlobHeader header = ReadHeader(snapshotKey, frameSpan.Slice(PreludeLength, headerLength));
        ValidateHeader(snapshotKey, header);

        if (!KnownPayloadSerializerIds.Contains(header.PayloadSerializerId))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.UnknownPayloadSerializer,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because payload serializer id '{header.PayloadSerializerId}' is not registered in the current process.");
        }

        SnapshotBlobCompression compression = ParseCompression(snapshotKey, header.CompressionAlgorithm);
        ReadOnlySpan<byte> storedPayloadSpan = frameSpan[(PreludeLength + headerLength)..];
        if (storedPayloadSpan.Length != header.StoredPayloadBytes)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidStoredPayloadLength,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because payload length '{storedPayloadSpan.Length}' does not match stored metadata '{header.StoredPayloadBytes}'.");
        }

        byte[] payloadBytes = RestorePayload(snapshotKey, compression, storedPayloadSpan);
        if (payloadBytes.Length != header.UncompressedPayloadBytes)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidUncompressedPayloadLength,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because restored payload length '{payloadBytes.Length}' does not match stored metadata '{header.UncompressedPayloadBytes}'.");
        }

        string payloadChecksum = ComputeSha256Hex(payloadBytes);
        if (!string.Equals(payloadChecksum, header.PayloadSha256, StringComparison.Ordinal))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.PayloadChecksumMismatch,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the payload checksum does not match the stored checksum.");
        }

        SnapshotEnvelope snapshot = new()
        {
            Data = ImmutableArray.Create(payloadBytes),
            DataContentType = header.DataContentType,
            DataSizeBytes = payloadBytes.Length,
            ReducerHash = header.ReducerHash,
        };

        return new(header, snapshot, compression);
    }

    /// <inheritdoc />
    public byte[] Encode(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        SnapshotPayloadSerializerDescriptor payloadSerializerDescriptor =
            PayloadSerializerResolver.ResolveConfiguredSerializerDescriptor();
        byte[] payloadBytes = snapshot.Data.ToArray();
        byte[] storedPayloadBytes = CompressPayload(payloadBytes, Options.Compression);
        StoredSnapshotBlobHeader header = new()
        {
            CanonicalStreamIdentity = BlobNameStrategy.GetCanonicalStreamIdentity(snapshotKey.Stream),
            CompressionAlgorithm = GetCompressionAlgorithm(Options.Compression),
            DataContentType = snapshot.DataContentType,
            PayloadSerializerId = payloadSerializerDescriptor.SerializerId,
            PayloadSha256 = ComputeSha256Hex(payloadBytes),
            ReducerHash = snapshot.ReducerHash,
            SnapshotStorageName = snapshotKey.Stream.SnapshotStorageName,
            StoredPayloadBytes = storedPayloadBytes.Length,
            StorageFormatVersion = CurrentStorageFormatVersion,
            UncompressedPayloadBytes = payloadBytes.Length,
            Version = snapshotKey.Version,
            WrittenUtc = TimeProvider.GetUtcNow(),
        };

        byte[] headerBytes = JsonSerializer.SerializeToUtf8Bytes(header, HeaderSerializerOptions);
        if ((headerBytes.Length == 0) || (headerBytes.Length > Options.MaxHeaderBytes))
        {
            throw new InvalidOperationException(
                $"Stored Blob frame header length '{headerBytes.Length}' is invalid for snapshot '{snapshotKey}'.");
        }

        byte[] frameBytes = new byte[PreludeLength + headerBytes.Length + storedPayloadBytes.Length];
        Span<byte> frameSpan = frameBytes.AsSpan();
        MagicBytes.CopyTo(frameSpan);
        BinaryPrimitives.WriteUInt16LittleEndian(frameSpan.Slice(8, sizeof(ushort)), SupportedFrameVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(frameSpan.Slice(10, sizeof(ushort)), SupportedFlags);
        BinaryPrimitives.WriteUInt32LittleEndian(frameSpan.Slice(12, sizeof(uint)), checked((uint)headerBytes.Length));
        headerBytes.CopyTo(frameSpan[PreludeLength..]);
        storedPayloadBytes.CopyTo(frameSpan[(PreludeLength + headerBytes.Length)..]);
        return frameBytes;
    }

    private static byte[] CompressPayload(
        byte[] payloadBytes,
        SnapshotBlobCompression compression
    )
    {
        return compression switch
        {
            SnapshotBlobCompression.Off => payloadBytes,
            SnapshotBlobCompression.Gzip => CompressWithGzip(payloadBytes),
            _ => throw new InvalidOperationException(
                $"Compression mode '{compression}' is not supported for stored Blob frames."),
        };
    }

    private static byte[] CompressWithGzip(
        byte[] payloadBytes
    )
    {
        using MemoryStream destination = new();
        using (GZipStream gzipStream = new(destination, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzipStream.Write(payloadBytes, 0, payloadBytes.Length);
        }

        return destination.ToArray();
    }

    private static string ComputeSha256Hex(
        byte[] payloadBytes
    ) => Convert.ToHexString(SHA256.HashData(payloadBytes));

    private static SnapshotBlobUnreadableFrameException CreateUnreadableException(
        SnapshotKey snapshotKey,
        SnapshotBlobUnreadableFrameReason reason,
        string message,
        Exception? innerException = null
    ) => new(snapshotKey, reason, message, innerException);

    private static string GetCompressionAlgorithm(
        SnapshotBlobCompression compression
    ) => compression switch
    {
        SnapshotBlobCompression.Off => "off",
        SnapshotBlobCompression.Gzip => "gzip",
        _ => throw new InvalidOperationException(
            $"Compression mode '{compression}' is not supported for stored Blob frames."),
    };

    private static SnapshotBlobCompression ParseCompression(
        SnapshotKey snapshotKey,
        string compressionAlgorithm
    ) => compressionAlgorithm switch
    {
        "off" => SnapshotBlobCompression.Off,
        "gzip" => SnapshotBlobCompression.Gzip,
        _ => throw CreateUnreadableException(
            snapshotKey,
            SnapshotBlobUnreadableFrameReason.UnknownCompressionAlgorithm,
            $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because compression algorithm '{compressionAlgorithm}' is not supported."),
    };

    private static StoredSnapshotBlobHeader ReadHeader(
        SnapshotKey snapshotKey,
        ReadOnlySpan<byte> headerBytes
    )
    {
        try
        {
            StoredSnapshotBlobHeader? header = JsonSerializer.Deserialize<StoredSnapshotBlobHeader>(headerBytes, HeaderSerializerOptions);
            return header ?? throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidHeader,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the frame header JSON is empty.");
        }
        catch (JsonException exception)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidHeader,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the frame header JSON could not be parsed.",
                exception);
        }
    }

    private static byte[] RestorePayload(
        SnapshotKey snapshotKey,
        SnapshotBlobCompression compression,
        ReadOnlySpan<byte> storedPayloadBytes
    )
    {
        return compression switch
        {
            SnapshotBlobCompression.Off => storedPayloadBytes.ToArray(),
            SnapshotBlobCompression.Gzip => DecompressWithGzip(snapshotKey, storedPayloadBytes),
            _ => throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.UnknownCompressionAlgorithm,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the payload compression algorithm is not supported."),
        };
    }

    private static byte[] DecompressWithGzip(
        SnapshotKey snapshotKey,
        ReadOnlySpan<byte> storedPayloadBytes
    )
    {
        try
        {
            using MemoryStream source = new(storedPayloadBytes.ToArray(), writable: false);
            using GZipStream gzipStream = new(source, CompressionMode.Decompress);
            using MemoryStream destination = new();
            gzipStream.CopyTo(destination);
            return destination.ToArray();
        }
        catch (InvalidDataException exception)
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidCompressedPayload,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the gzip payload could not be decompressed.",
                exception);
        }
    }

    private void ValidateHeader(
        SnapshotKey snapshotKey,
        StoredSnapshotBlobHeader header
    )
    {
        if ((header.StorageFormatVersion != CurrentStorageFormatVersion)
            || string.IsNullOrWhiteSpace(header.CanonicalStreamIdentity)
            || string.IsNullOrWhiteSpace(header.SnapshotStorageName)
            || string.IsNullOrWhiteSpace(header.ReducerHash)
            || string.IsNullOrWhiteSpace(header.DataContentType)
            || string.IsNullOrWhiteSpace(header.PayloadSerializerId)
            || string.IsNullOrWhiteSpace(header.CompressionAlgorithm)
            || string.IsNullOrWhiteSpace(header.PayloadSha256)
            || (header.Version < 0)
            || (header.StoredPayloadBytes < 0)
            || (header.UncompressedPayloadBytes < 0))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.InvalidHeaderValues,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the frame header is missing required values or contains invalid lengths." );
        }

        string expectedCanonicalStreamIdentity = BlobNameStrategy.GetCanonicalStreamIdentity(snapshotKey.Stream);
        if (!string.Equals(header.CanonicalStreamIdentity, expectedCanonicalStreamIdentity, StringComparison.Ordinal)
            || (header.Version != snapshotKey.Version)
            || !string.Equals(header.SnapshotStorageName, snapshotKey.Stream.SnapshotStorageName, StringComparison.Ordinal)
            || !string.Equals(header.ReducerHash, snapshotKey.Stream.ReducersHash, StringComparison.Ordinal))
        {
            throw CreateUnreadableException(
                snapshotKey,
                SnapshotBlobUnreadableFrameReason.UnexpectedSnapshotIdentity,
                $"Stored Blob frame for snapshot '{snapshotKey}' is unreadable because the persisted stream identity or version does not match the requested snapshot.");
        }
    }
}