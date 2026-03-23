using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;
using Mississippi.Tributary.Runtime.Storage.Blob.Startup;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;
using Xunit.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Verifies stored Blob frame encode and decode behavior for increment 3.
/// </summary>
public sealed class BlobEnvelopeCodecTests
{
    private readonly ITestOutputHelper testOutputHelper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobEnvelopeCodecTests" /> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper.</param>
    public BlobEnvelopeCodecTests(
        ITestOutputHelper testOutputHelper
    ) =>
        this.testOutputHelper = testOutputHelper;

    /// <summary>
    ///     Verifies stored frames round-trip with both supported compression modes.
    /// </summary>
    /// <param name="compressionValue">The configured payload compression mode value.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EncodeAndDecodeShouldRoundTripSnapshotEnvelope(
        int compressionValue
    )
    {
        SnapshotBlobCompression compression = (SnapshotBlobCompression)compressionValue;
        SnapshotKey snapshotKey = CreateSnapshotKey();
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope(4096);
        FakeTimeProvider timeProvider = new();
        DateTimeOffset expectedWrittenUtc = new(2026, 3, 23, 12, 34, 56, TimeSpan.Zero);
        timeProvider.SetUtcNow(expectedWrittenUtc);
        BlobEnvelopeCodec codec = CreateCodec(
            [new TestSerializationProvider("System.Text.Json")],
            compression,
            timeProvider: timeProvider);

        byte[] storedFrame = codec.Encode(snapshotKey, snapshot);
        DecodedSnapshotBlobFrame decodedFrame = codec.Decode(snapshotKey, storedFrame);

        Assert.Equal(snapshot.DataContentType, decodedFrame.Snapshot.DataContentType);
        Assert.Equal(snapshot.ReducerHash, decodedFrame.Snapshot.ReducerHash);
        Assert.Equal(snapshot.DataSizeBytes, decodedFrame.Snapshot.DataSizeBytes);
        Assert.True(snapshot.Data.SequenceEqual(decodedFrame.Snapshot.Data));
        Assert.Equal(compression, decodedFrame.Compression);
        Assert.Equal(expectedWrittenUtc, decodedFrame.Header.WrittenUtc);
        Assert.Equal("Mississippi.Tributary.Runtime.Storage.Blob.L0Tests.TestSerializationProvider", decodedFrame.Header.PayloadSerializerId);
        Assert.Equal(snapshotKey.Stream.SnapshotStorageName, decodedFrame.Header.SnapshotStorageName);
    }

    /// <summary>
    ///     Verifies the persisted serializer identity survives a new codec instance with the same concrete provider type.
    /// </summary>
    [Fact]
    public void DecodeShouldAcceptPersistedSerializerIdentityAcrossCodecInstances()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope(1024);
        BlobEnvelopeCodec writeCodec = CreateCodec(
            [new AlternateTestSerializationProvider("custom-json")],
            payloadSerializerFormat: "custom-json");
        BlobEnvelopeCodec readCodec = CreateCodec(
            [new AlternateTestSerializationProvider("custom-json")],
            payloadSerializerFormat: "custom-json");

        byte[] storedFrame = writeCodec.Encode(snapshotKey, snapshot);
        DecodedSnapshotBlobFrame decodedFrame = readCodec.Decode(snapshotKey, storedFrame);

        Assert.Equal(
            "Mississippi.Tributary.Runtime.Storage.Blob.L0Tests.AlternateTestSerializationProvider",
            decodedFrame.Header.PayloadSerializerId);
    }

    /// <summary>
    ///     Verifies unknown persisted serializer identities fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailForUnknownPersistedSerializerIdentity()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope(1024);
        BlobEnvelopeCodec writeCodec = CreateCodec(
            [new AlternateTestSerializationProvider("custom-json")],
            payloadSerializerFormat: "custom-json");
        BlobEnvelopeCodec readCodec = CreateCodec(
            [new TestSerializationProvider("custom-json")],
            payloadSerializerFormat: "custom-json");

        byte[] storedFrame = writeCodec.Encode(snapshotKey, snapshot);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => readCodec.Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.UnknownPayloadSerializer, exception.Reason);
        Assert.Contains("payload serializer id", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies invalid magic bytes fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenFrameMagicIsInvalid()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        storedFrame[0] = (byte)'X';

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidMagic, exception.Reason);
    }

    /// <summary>
    ///     Verifies unknown frame versions fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenFrameVersionIsUnknown()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        storedFrame[8] = 2;
        storedFrame[9] = 0;

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.UnsupportedFrameVersion, exception.Reason);
    }

    /// <summary>
    ///     Verifies truncated preludes fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenPreludeIsTruncated()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        Array.Resize(ref storedFrame, 15);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.TruncatedPrelude, exception.Reason);
    }

    /// <summary>
    ///     Verifies non-zero reserved flags fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenFlagsAreNotSupported()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        storedFrame[10] = 1;
        storedFrame[11] = 0;

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.UnsupportedFlags, exception.Reason);
    }

    /// <summary>
    ///     Verifies oversized headers fail closed against the configured maximum.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenHeaderLengthExceedsConfiguredMaximum()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(
            snapshotKey,
            CreateSnapshotEnvelope(1024));

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")], maxHeaderBytes: 32).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidHeaderLength, exception.Reason);
    }

    /// <summary>
    ///     Verifies persisted header lengths above <see cref="int.MaxValue" /> fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenHeaderLengthOverflowsInt32()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(
            snapshotKey,
            CreateSnapshotEnvelope(1024));
        BinaryPrimitives.WriteUInt32LittleEndian(storedFrame.AsSpan(12, sizeof(uint)), uint.MaxValue);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidHeaderLength, exception.Reason);
    }

    /// <summary>
    ///     Verifies malformed header JSON fails closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenHeaderJsonIsMalformed()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        storedFrame[16] = (byte)'!';

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidHeader, exception.Reason);
    }

    /// <summary>
    ///     Verifies missing required header values fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenHeaderValuesAreInvalid()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        ReplaceFirst(
            storedFrame,
            System.Text.Encoding.UTF8.GetBytes("\"application/json\""),
            System.Text.Encoding.UTF8.GetBytes("\"                \""));

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidHeaderValues, exception.Reason);
    }

    /// <summary>
    ///     Verifies header identity mismatches fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenHeaderSnapshotIdentityDoesNotMatchRequestedKey()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        SnapshotKey differentKey = new(new("brook-a", "projection-a", "entity-99", "reducers-v1"), snapshotKey.Version);
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(differentKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.UnexpectedSnapshotIdentity, exception.Reason);
    }

    /// <summary>
    ///     Verifies unknown compression algorithms fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenCompressionAlgorithmIsUnknown()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        byte[] original = System.Text.Encoding.UTF8.GetBytes("\"off\"");
        byte[] replacement = System.Text.Encoding.UTF8.GetBytes("\"bad\"");
        ReplaceFirst(storedFrame, original, replacement);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.UnknownCompressionAlgorithm, exception.Reason);
    }

    /// <summary>
    ///     Verifies stored payload length mismatches fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenStoredPayloadLengthDoesNotMatchHeader()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        Array.Resize(ref storedFrame, storedFrame.Length - 1);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidStoredPayloadLength, exception.Reason);
    }

    /// <summary>
    ///     Verifies invalid gzip payload bytes fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenCompressedPayloadIsInvalid()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec(
            [new TestSerializationProvider("System.Text.Json")],
            SnapshotBlobCompression.Gzip).Encode(snapshotKey, CreateSnapshotEnvelope(4096));
        int payloadStartOffset = GetStoredPayloadStartOffset(storedFrame);
        storedFrame[payloadStartOffset] = 0;
        storedFrame[payloadStartOffset + 1] = 0;

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")], SnapshotBlobCompression.Gzip).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidCompressedPayload, exception.Reason);
    }

    /// <summary>
    ///     Verifies uncompressed payload length mismatches fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenUncompressedPayloadLengthDoesNotMatchHeader()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        ReplaceFirst(
            storedFrame,
            System.Text.Encoding.UTF8.GetBytes("\"uncompressedPayloadBytes\":512"),
            System.Text.Encoding.UTF8.GetBytes("\"uncompressedPayloadBytes\":513"));

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.InvalidUncompressedPayloadLength, exception.Reason);
    }

    /// <summary>
    ///     Verifies payload checksum mismatches fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenPayloadChecksumDoesNotMatch()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec([new TestSerializationProvider("System.Text.Json")]).Encode(snapshotKey, CreateSnapshotEnvelope(512));
        storedFrame[^1] ^= 0x5A;

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")]).Decode(snapshotKey, storedFrame));

        Assert.Equal(SnapshotBlobUnreadableFrameReason.PayloadChecksumMismatch, exception.Reason);
    }

    /// <summary>
    ///     Verifies truncated gzip payloads fail closed.
    /// </summary>
    [Fact]
    public void DecodeShouldFailWhenGzipPayloadIsTruncated()
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        byte[] storedFrame = CreateCodec(
            [new TestSerializationProvider("System.Text.Json")],
            SnapshotBlobCompression.Gzip).Encode(snapshotKey, CreateSnapshotEnvelope(4096));
        Array.Resize(ref storedFrame, storedFrame.Length - 8);

        SnapshotBlobUnreadableFrameException exception = Assert.Throws<SnapshotBlobUnreadableFrameException>(
            () => CreateCodec([new TestSerializationProvider("System.Text.Json")], SnapshotBlobCompression.Gzip).Decode(snapshotKey, storedFrame));

        Assert.Contains(
            exception.Reason,
            new[]
            {
                SnapshotBlobUnreadableFrameReason.InvalidStoredPayloadLength,
                SnapshotBlobUnreadableFrameReason.InvalidCompressedPayload,
            });
    }

    /// <summary>
    ///     Verifies the deterministic large-payload matrix round-trips with bounded buffering and measurable frame sizes.
    /// </summary>
    /// <param name="payloadBytes">The deterministic payload size in bytes.</param>
    [Theory]
    [InlineData(256 * 1024)]
    [InlineData(1024 * 1024)]
    [InlineData(5 * 1024 * 1024)]
    [InlineData(16 * 1024 * 1024)]
    public void LargePayloadMatrixShouldRoundTripDeterministicPayloadSizes(
        int payloadBytes
    )
    {
        SnapshotKey snapshotKey = CreateSnapshotKey();
        SnapshotEnvelope snapshot = CreateSnapshotEnvelope(payloadBytes);
        BlobEnvelopeCodec codec = CreateCodec(
            [new TestSerializationProvider("System.Text.Json")],
            SnapshotBlobCompression.Gzip);

        byte[] storedFrame = codec.Encode(snapshotKey, snapshot);
        DecodedSnapshotBlobFrame decodedFrame = codec.Decode(snapshotKey, storedFrame);

        Assert.Equal(payloadBytes, decodedFrame.Snapshot.DataSizeBytes);
        Assert.True(snapshot.Data.SequenceEqual(decodedFrame.Snapshot.Data));

        this.testOutputHelper.WriteLine(
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"payload={payloadBytes}; compression=gzip; uploaded={storedFrame.Length}; downloaded={storedFrame.Length}; restored={decodedFrame.Snapshot.DataSizeBytes}; buffering=payload-buffer + optional-compressed-buffer + final-frame-buffer"));
    }

    private static SnapshotEnvelope CreateSnapshotEnvelope(
        int payloadBytes
    )
    {
        byte[] payload = Enumerable.Range(0, payloadBytes)
            .Select(index => (byte)(index % 251))
            .ToArray();

        return new SnapshotEnvelope
        {
            Data = ImmutableArray.Create(payload),
            DataContentType = "application/json",
            DataSizeBytes = payloadBytes,
            ReducerHash = "reducers-v1",
        };
    }

    private static SnapshotKey CreateSnapshotKey() =>
        new(new SnapshotStreamKey("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);

    private static BlobEnvelopeCodec CreateCodec(
        IReadOnlyCollection<ISerializationProvider> serializationProviders,
        SnapshotBlobCompression compression = SnapshotBlobCompression.Off,
        int? maxHeaderBytes = null,
        string? payloadSerializerFormat = null,
        FakeTimeProvider? timeProvider = null
    )
    {
        SnapshotBlobStorageOptions options = new()
        {
            Compression = compression,
            PayloadSerializerFormat = payloadSerializerFormat ?? SnapshotBlobDefaults.PayloadSerializerFormat,
        };
        if (maxHeaderBytes.HasValue)
        {
            options.MaxHeaderBytes = maxHeaderBytes.Value;
        }

        return new BlobEnvelopeCodec(
            new BlobNameStrategy(Options.Create(options)),
            new SnapshotPayloadSerializerResolver(serializationProviders, Options.Create(options)),
            serializationProviders,
            Options.Create(options),
            timeProvider ?? new FakeTimeProvider());
    }

    private static void ReplaceFirst(
        byte[] target,
        byte[] original,
        byte[] replacement
    )
    {
        int index = FindFirst(target, original);
        Assert.NotEqual(-1, index);
        Assert.Equal(original.Length, replacement.Length);
        Array.Copy(replacement, 0, target, index, replacement.Length);
    }

    private static int FindFirst(
        byte[] haystack,
        byte[] needle
    )
    {
        for (int index = 0; index <= (haystack.Length - needle.Length); index++)
        {
            bool match = true;
            for (int needleIndex = 0; needleIndex < needle.Length; needleIndex++)
            {
                if (haystack[index + needleIndex] != needle[needleIndex])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return index;
            }
        }

        return -1;
    }

    private static int GetStoredPayloadStartOffset(
        byte[] storedFrame
    ) => 16 + checked((int)BinaryPrimitives.ReadUInt32LittleEndian(storedFrame.AsSpan(12, sizeof(uint))));
}