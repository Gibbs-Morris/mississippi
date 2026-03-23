using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Azure.Storage.Blobs.Models;

using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Azurite-backed trust slice proving canonical Blob snapshot registration, gzip framing, and Blob-backed restart
///     hydration.
/// </summary>
public sealed class BlobSnapshotTrustSliceTests
{
    private const ushort FrameFlags = 0;

    private const int FramePreludeLength = 16;

    private const ushort FrameVersion = 1;

    private const int MinimumExpectedUncompressedPayloadBytes = 512 * 1024;

    private const int TargetPayloadCharacters = 600 * 1024;

    private static readonly JsonSerializerOptions HeaderSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static ReadOnlySpan<byte> FrameMagicBytes => "TRIBSNAP"u8;

    private static byte[] CompressPayload(
        byte[] payloadBytes,
        string compressionAlgorithm
    ) =>
        compressionAlgorithm switch
        {
            "off" => payloadBytes,
            "gzip" => CompressWithGzip(payloadBytes),
            var _ => throw new InvalidOperationException(
                $"Unsupported compression algorithm '{compressionAlgorithm}'."),
        };

    private static byte[] CompressWithGzip(
        byte[] payloadBytes
    )
    {
        using MemoryStream destination = new();
        using (GZipStream gzipStream = new(destination, CompressionLevel.SmallestSize, true))
        {
            gzipStream.Write(payloadBytes, 0, payloadBytes.Length);
        }

        return destination.ToArray();
    }

    private static string ComputePayloadSha256(
        byte[] payloadBytes
    ) =>
        Convert.ToHexString(SHA256.HashData(payloadBytes));

    private static string CreateLargePayload(
        int targetLength,
        int salt = 31
    )
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] buffer = new char[targetLength];
        for (int index = 0; index < buffer.Length; index++)
        {
            buffer[index] = Alphabet[((index * salt) + (index / 7)) % Alphabet.Length];
        }

        return new(buffer);
    }

    private static byte[] EncodeSnapshotBlobFrame(
        PersistedSnapshotBlobHeader header,
        byte[] storedPayloadBytes
    )
    {
        byte[] headerBytes = JsonSerializer.SerializeToUtf8Bytes(header);
        byte[] frameBytes = new byte[FramePreludeLength + headerBytes.Length + storedPayloadBytes.Length];
        Span<byte> frameSpan = frameBytes;
        FrameMagicBytes.CopyTo(frameSpan);
        BinaryPrimitives.WriteUInt16LittleEndian(frameSpan.Slice(8, sizeof(ushort)), FrameVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(frameSpan.Slice(10, sizeof(ushort)), FrameFlags);
        BinaryPrimitives.WriteUInt32LittleEndian(frameSpan.Slice(12, sizeof(uint)), checked((uint)headerBytes.Length));
        headerBytes.CopyTo(frameSpan[FramePreludeLength..]);
        storedPayloadBytes.CopyTo(frameSpan[(FramePreludeLength + headerBytes.Length)..]);
        return frameBytes;
    }

    private static async Task<BlobFrameInspection> InspectSnapshotBlobAsync(
        BlobClient blobClient
    )
    {
        BlobDownloadResult download = (await blobClient.DownloadContentAsync()).Value;
        byte[] frame = download.Content.ToArray();
        frame.Length.Should().BeGreaterThan(FramePreludeLength, "the stored frame must contain a prelude and payload");
        frame.AsSpan(0, FrameMagicBytes.Length).ToArray().Should().Equal(FrameMagicBytes.ToArray());
        int headerLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(12, sizeof(uint))));
        PersistedSnapshotBlobHeader header = JsonSerializer.Deserialize<PersistedSnapshotBlobHeader>(
                                                 frame.AsSpan(FramePreludeLength, headerLength),
                                                 HeaderSerializerOptions) ??
                                             throw new InvalidOperationException(
                                                 "The snapshot blob header could not be parsed.");
        byte[] payloadPrefix = frame.Skip(FramePreludeLength + headerLength).Take(2).ToArray();
        return new(header, payloadPrefix);
    }

    private static async Task OverwriteSnapshotBlobAsync(
        BlobClient blobClient,
        PersistedSnapshotBlobHeader header,
        LargeSnapshotAggregate snapshotState
    )
    {
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(snapshotState);
        byte[] storedPayloadBytes = CompressPayload(payloadBytes, header.CompressionAlgorithm);
        PersistedSnapshotBlobHeader updatedHeader = header with
        {
            PayloadSha256 = ComputePayloadSha256(payloadBytes),
            StoredPayloadBytes = storedPayloadBytes.Length,
            UncompressedPayloadBytes = payloadBytes.Length,
        };
        byte[] updatedFrame = EncodeSnapshotBlobFrame(updatedHeader, storedPayloadBytes);
        using MemoryStream stream = new(updatedFrame, false);
        await blobClient.UploadAsync(stream, true);
    }

    private sealed record BlobFrameInspection(PersistedSnapshotBlobHeader Header, IReadOnlyList<byte> PayloadPrefix);

    private sealed record PersistedSnapshotBlobHeader
    {
        [JsonPropertyName("canonicalStreamIdentity")]
        public string CanonicalStreamIdentity { get; init; } = string.Empty;

        [JsonPropertyName("compressionAlgorithm")]
        public string CompressionAlgorithm { get; init; } = string.Empty;

        [JsonPropertyName("dataContentType")]
        public string DataContentType { get; init; } = string.Empty;

        [JsonPropertyName("payloadSerializerId")]
        public string PayloadSerializerId { get; init; } = string.Empty;

        [JsonPropertyName("payloadSha256")]
        public string PayloadSha256 { get; init; } = string.Empty;

        [JsonPropertyName("reducerHash")]
        public string ReducerHash { get; init; } = string.Empty;

        [JsonPropertyName("snapshotStorageName")]
        public string SnapshotStorageName { get; init; } = string.Empty;

        [JsonPropertyName("storageFormatVersion")]
        public int StorageFormatVersion { get; init; }

        [JsonPropertyName("storedPayloadBytes")]
        public long StoredPayloadBytes { get; init; }

        [JsonPropertyName("uncompressedPayloadBytes")]
        public long UncompressedPayloadBytes { get; init; }

        [JsonPropertyName("version")]
        public long Version { get; init; }

        [JsonPropertyName("writtenUtc")]
        public DateTimeOffset WrittenUtc { get; init; }
    }

    /// <summary>
    ///     Proves a large snapshot written through the canonical Blob registration path is read back from Blob after restart.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task LargeSnapshotShouldSurviveRestartWithGzipAndNonDefaultSerializer()
    {
        await using BlobSnapshotTrustSliceScenario scenario = await BlobSnapshotTrustSliceScenario.StartAsync();
        string entityId = $"large-snapshot-{Guid.NewGuid():N}";
        string marker = $"marker-{Guid.NewGuid():N}";
        string payload = CreateLargePayload(TargetPayloadCharacters);
        IGenericAggregateGrain<LargeSnapshotAggregate> aggregate = scenario.AggregateGrainFactory
            .GetGenericAggregate<LargeSnapshotAggregate>(entityId);
        OperationResult writeResult = await aggregate.ExecuteAsync(
            new StoreLargeSnapshotCommand
            {
                Marker = marker,
                Payload = payload,
            });
        writeResult.Success.Should().BeTrue("the large snapshot command should succeed");
        LargeSnapshotAggregate? stateBeforeRestart = await aggregate.GetStateAsync();
        stateBeforeRestart.Should().NotBeNull();
        stateBeforeRestart!.Marker.Should().Be(marker);
        stateBeforeRestart!.Payload.Should().Be(payload);
        stateBeforeRestart.PayloadLength.Should().Be(payload.Length);
        BlobClient snapshotBlob = await scenario.PersistSnapshotAsync(entityId, stateBeforeRestart);
        BlobFrameInspection inspection = await InspectSnapshotBlobAsync(snapshotBlob);
        inspection.Header.CompressionAlgorithm.Should().Be("gzip");
        inspection.Header.PayloadSerializerId.Should().Be(typeof(CrescentBlobCustomJsonSerializationProvider).FullName);
        inspection.Header.UncompressedPayloadBytes.Should().BeGreaterThan(MinimumExpectedUncompressedPayloadBytes);
        inspection.Header.StoredPayloadBytes.Should().BeLessThan(inspection.Header.UncompressedPayloadBytes);
        inspection.PayloadPrefix.Should().Equal(0x1F, 0x8B);
        string blobBackedMarker = $"blob-backed-{Guid.NewGuid():N}";
        string blobBackedPayload = CreateLargePayload(payload.Length, 17);
        blobBackedPayload.Should()
            .NotBe(payload, "the restart assertion must distinguish Blob reload from event replay");
        LargeSnapshotAggregate blobBackedSnapshotState = stateBeforeRestart with
        {
            Marker = blobBackedMarker,
            Payload = blobBackedPayload,
            PayloadLength = blobBackedPayload.Length,
        };
        await OverwriteSnapshotBlobAsync(snapshotBlob, inspection.Header, blobBackedSnapshotState);
        await scenario.RestartOrleansAsync();
        IGenericAggregateGrain<LargeSnapshotAggregate> restartedAggregate = scenario.AggregateGrainFactory
            .GetGenericAggregate<LargeSnapshotAggregate>(entityId);
        LargeSnapshotAggregate? stateAfterRestart = await restartedAggregate.GetStateAsync();
        stateAfterRestart.Should().NotBeNull();
        stateAfterRestart!.Marker.Should().Be(blobBackedSnapshotState.Marker);
        stateAfterRestart.Payload.Should().Be(blobBackedSnapshotState.Payload);
        stateAfterRestart.PayloadLength.Should().Be(blobBackedSnapshotState.PayloadLength);
        stateAfterRestart.Marker.Should().NotBe(marker);
        stateAfterRestart.Payload.Should().NotBe(payload);
    }
}