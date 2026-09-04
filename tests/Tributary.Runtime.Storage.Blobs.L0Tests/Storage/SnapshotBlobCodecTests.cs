using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs;
using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests.Storage;

/// <summary>
///     Tests document validation and snapshot envelope conversion independently of Blob storage.
/// </summary>
public sealed class SnapshotBlobCodecTests
{
    private static readonly SnapshotKey SnapshotKey = new(new("TEST.BROOK", "Balance", "account-1", "reducers"), 7);

    private static SnapshotBlobCodec CreateCodec(
        Action<SnapshotBlobStorageOptions>? configure = null
    )
    {
        SnapshotBlobStorageOptions options = new();
        configure?.Invoke(options);
        return new(Options.Create(options));
    }

    private static JsonObject CreateDocumentJson() =>
        JsonNode.Parse(CreateCodec().Encode(SnapshotKey, CreateEnvelope()).ToString())!.AsObject();

    private static SnapshotEnvelope CreateEnvelope() =>
        new()
        {
            Data = ImmutableArray.CreateRange(Encoding.UTF8.GetBytes("snapshot payload")),
            DataContentType = "application/x-test",
            DataSizeBytes = 16,
            ReducerHash = SnapshotKey.Stream.ReducersHash,
        };

    /// <summary>
    ///     Verifies a document exactly at the configured byte limit remains readable and writable.
    /// </summary>
    [Fact]
    public void CodecShouldAcceptDocumentAtConfiguredMaximum()
    {
        SnapshotEnvelope envelope = CreateEnvelope();
        BinaryData document = CreateCodec().Encode(SnapshotKey, envelope);
        SnapshotBlobCodec codec =
            CreateCodec(options => options.MaximumSnapshotDocumentSizeBytes = document.ToMemory().Length);
        BinaryData encoded = codec.Encode(SnapshotKey, envelope);
        SnapshotEnvelope decoded = codec.Decode(SnapshotKey, encoded);
        Assert.Equal(document.ToMemory().Length, encoded.ToMemory().Length);
        Assert.Equal(envelope.Data.AsSpan().ToArray(), decoded.Data.AsSpan().ToArray());
    }

    /// <summary>
    ///     Verifies zero-byte payloads are accepted when explicitly present in the document.
    /// </summary>
    [Fact]
    public void CodecShouldRoundTripEmptyPayload()
    {
        SnapshotBlobCodec codec = CreateCodec();
        SnapshotEnvelope envelope = CreateEnvelope() with
        {
            Data = ImmutableArray<byte>.Empty,
            DataSizeBytes = 0,
        };
        SnapshotEnvelope decoded = codec.Decode(SnapshotKey, codec.Encode(SnapshotKey, envelope));
        Assert.Empty(decoded.Data);
        Assert.Equal(0, decoded.DataSizeBytes);
    }

    /// <summary>
    ///     Verifies metadata and payload bytes survive both storage encodings at the payload limit.
    /// </summary>
    /// <param name="enableCompression">Whether the payload is gzip-compressed.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodecShouldRoundTripEnvelopeAtPayloadLimit(
        bool enableCompression
    )
    {
        SnapshotEnvelope envelope = CreateEnvelope();
        SnapshotBlobCodec codec = CreateCodec(options =>
        {
            options.EnableCompression = enableCompression;
            options.MaximumSnapshotPayloadSizeBytes = envelope.DataSizeBytes;
        });
        BinaryData encoded = codec.Encode(SnapshotKey, envelope);
        SnapshotEnvelope decoded = codec.Decode(SnapshotKey, encoded);
        SnapshotBlobDocument document = SnapshotBlobDocumentSerializer.Deserialize(encoded);
        Assert.Equal(
            enableCompression ? SnapshotBlobCompression.Gzip : SnapshotBlobCompression.None,
            document.Compression);
        Assert.Equal(envelope.Data.AsSpan().ToArray(), decoded.Data.AsSpan().ToArray());
        Assert.Equal(envelope.DataContentType, decoded.DataContentType);
        Assert.Equal(envelope.DataSizeBytes, decoded.DataSizeBytes);
        Assert.Equal(envelope.ReducerHash, decoded.ReducerHash);
    }

    /// <summary>
    ///     Verifies constructor options are required.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenOptionsAreNull() =>
        Assert.Throws<ArgumentNullException>(() => new SnapshotBlobCodec(null!));

    /// <summary>
    ///     Verifies a payload that expands past its configured bound retains the size-limit diagnostic.
    /// </summary>
    [Fact]
    public void DecodeShouldPreserveDecompressionSizeLimitDiagnostic()
    {
        SnapshotBlobCodec writer = CreateCodec(options => options.EnableCompression = true);
        JsonObject json = JsonNode.Parse(writer.Encode(SnapshotKey, CreateEnvelope()).ToString())!.AsObject();
        json["dataSizeBytes"] = 1;
        SnapshotBlobCodec reader = CreateCodec(options => options.MaximumSnapshotPayloadSizeBytes = 1);
        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(() => reader.Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("configured maximum '1'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the document version is checked independently of stream identity.
    /// </summary>
    [Fact]
    public void DecodeShouldRejectDifferentSnapshotVersion()
    {
        JsonObject json = CreateDocumentJson();
        json["version"] = SnapshotKey.Version + 1;
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            CreateCodec().Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies decoded document identity matches every component of the requested key.
    /// </summary>
    /// <param name="propertyName">The altered identity property.</param>
    [Theory]
    [InlineData("brookName")]
    [InlineData("snapshotStorageName")]
    [InlineData("entityId")]
    [InlineData("reducersHash")]
    public void DecodeShouldRejectDifferentStreamIdentity(
        string propertyName
    )
    {
        JsonObject json = CreateDocumentJson();
        json[propertyName] = "different";
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            CreateCodec().Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies invalid Base64 uses the invalid-data boundary with useful context.
    /// </summary>
    [Fact]
    public void DecodeShouldRejectInvalidBase64()
    {
        JsonObject json = CreateDocumentJson();
        json["data"] = "invalid-base64";
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            CreateCodec().Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("Base64", exception.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    /// <summary>
    ///     Verifies document size is checked before parsing malformed JSON.
    /// </summary>
    [Fact]
    public void DecodeShouldRejectOversizedDocumentBeforeDeserialization()
    {
        SnapshotBlobCodec codec = CreateCodec(options => options.MaximumSnapshotDocumentSizeBytes = 1);
        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(() => codec.Decode(SnapshotKey, new("{{")));
        Assert.Contains("document size", exception.Message, StringComparison.Ordinal);
        Assert.Contains("maximum '1'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies stored and uncompressed lengths are each validated.
    /// </summary>
    /// <param name="propertyName">The altered size property.</param>
    /// <param name="expectedMessage">The size mismatch diagnostic.</param>
    [Theory]
    [InlineData("storedSizeBytes", "Stored payload size mismatch")]
    [InlineData("dataSizeBytes", "Uncompressed payload size mismatch")]
    public void DecodeShouldRejectSizeMismatch(
        string propertyName,
        string expectedMessage
    )
    {
        JsonObject json = CreateDocumentJson();
        json[propertyName] = 0;
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            CreateCodec().Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies unsupported document schemas are rejected.
    /// </summary>
    [Fact]
    public void DecodeShouldRejectUnsupportedSchema()
    {
        JsonObject json = CreateDocumentJson();
        json["schemaVersion"] = SnapshotBlobDocument.CurrentSchemaVersion + 1;
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            CreateCodec().Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("unsupported schema", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies a null document is a caller argument error.
    /// </summary>
    [Fact]
    public void DecodeShouldThrowWhenDocumentIsNull() =>
        Assert.Throws<ArgumentNullException>(() => CreateCodec().Decode(SnapshotKey, null!));

    /// <summary>
    ///     Verifies declared payload limits are checked before allocating decoded Base64 data.
    /// </summary>
    /// <param name="declaredSize">An invalid declared uncompressed payload size.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void DecodeShouldValidateDeclaredPayloadSizeBeforeBase64(
        long declaredSize
    )
    {
        JsonObject json = CreateDocumentJson();
        json["dataSizeBytes"] = declaredSize;
        json["data"] = "invalid-base64";
        SnapshotBlobCodec codec = CreateCodec(options => options.MaximumSnapshotPayloadSizeBytes = 1);
        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(() => codec.Decode(SnapshotKey, new(json.ToJsonString())));
        Assert.Contains("DataSizeBytes", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding cannot persist a null content type that the reader would reject.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectNullContentType()
    {
        SnapshotEnvelope envelope = CreateEnvelope() with
        {
            DataContentType = null!,
        };
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => CreateCodec().Encode(SnapshotKey, envelope));
        Assert.Contains("DataContentType", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding cannot produce documents exceeding the reader's configured limit.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectOversizedDocument()
    {
        SnapshotEnvelope envelope = CreateEnvelope();
        int documentLength = CreateCodec().Encode(SnapshotKey, envelope).ToMemory().Length;
        SnapshotBlobCodec codec = CreateCodec(options => options.MaximumSnapshotDocumentSizeBytes = documentLength - 1);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => codec.Encode(SnapshotKey, envelope));
        Assert.Equal("snapshot", exception.ParamName);
        Assert.Contains("document size", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding rejects payloads larger than the configured maximum.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectOversizedPayload()
    {
        SnapshotBlobCodec codec = CreateCodec(options => options.MaximumSnapshotPayloadSizeBytes = 1);
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => codec.Encode(SnapshotKey, CreateEnvelope()));
        Assert.Contains("exceeds configured maximum '1'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding rejects invalid denormalized payload lengths.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectPayloadSizeMismatch()
    {
        SnapshotEnvelope envelope = CreateEnvelope() with
        {
            DataSizeBytes = 0,
        };
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => CreateCodec().Encode(SnapshotKey, envelope));
        Assert.Contains("does not match payload length", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding rejects envelopes built using a different reducer set.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectReducerHashMismatch()
    {
        SnapshotEnvelope envelope = CreateEnvelope() with
        {
            ReducerHash = "different",
        };
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => CreateCodec().Encode(SnapshotKey, envelope));
        Assert.Contains("does not match snapshot key", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies encoding rejects uninitialized immutable arrays with an argument error.
    /// </summary>
    [Fact]
    public void EncodeShouldRejectUninitializedPayload()
    {
        SnapshotEnvelope envelope = CreateEnvelope() with
        {
            Data = default,
        };
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => CreateCodec().Encode(SnapshotKey, envelope));
        Assert.Contains("Data must be initialized", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies a null envelope is a caller argument error.
    /// </summary>
    [Fact]
    public void EncodeShouldThrowWhenSnapshotIsNull() =>
        Assert.Throws<ArgumentNullException>(() => CreateCodec().Encode(SnapshotKey, null!));
}