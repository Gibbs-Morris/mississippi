using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for Blob snapshot JSON document serialization.
/// </summary>
public sealed class SnapshotBlobDocumentTests
{
    private static JsonObject CreateDocumentJson() =>
        JsonNode.Parse(SnapshotBlobDocumentSerializer.Serialize(new()).ToString())!.AsObject();

    /// <summary>
    ///     Verifies malformed JSON and the JSON null value follow the invalid-data boundary.
    /// </summary>
    /// <param name="json">The invalid document JSON.</param>
    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    public void DeserializeShouldRejectInvalidDocument(
        string json
    ) =>
        Assert.Throws<InvalidDataException>(() => SnapshotBlobDocumentSerializer.Deserialize(new(json)));

    /// <summary>
    ///     Verifies every persisted field must be present, even when its default value would otherwise be valid.
    /// </summary>
    /// <param name="propertyName">The required property omitted from the stored JSON.</param>
    [Theory]
    [InlineData("brookName")]
    [InlineData("compression")]
    [InlineData("data")]
    [InlineData("dataContentType")]
    [InlineData("dataSizeBytes")]
    [InlineData("entityId")]
    [InlineData("reducersHash")]
    [InlineData("schemaVersion")]
    [InlineData("snapshotStorageName")]
    [InlineData("storedSizeBytes")]
    [InlineData("version")]
    public void DeserializeShouldRejectMissingRequiredProperty(
        string propertyName
    )
    {
        JsonObject json = CreateDocumentJson();
        Assert.True(json.Remove(propertyName));
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            SnapshotBlobDocumentSerializer.Deserialize(new(json.ToJsonString())));
        Assert.IsType<JsonException>(exception.InnerException);
    }

    /// <summary>
    ///     Verifies persisted null strings cannot escape the serialization boundary.
    /// </summary>
    /// <param name="propertyName">The required string property replaced by null.</param>
    [Theory]
    [InlineData("brookName")]
    [InlineData("compression")]
    [InlineData("data")]
    [InlineData("dataContentType")]
    [InlineData("entityId")]
    [InlineData("reducersHash")]
    [InlineData("snapshotStorageName")]
    public void DeserializeShouldRejectNullStringProperty(
        string propertyName
    )
    {
        JsonObject json = CreateDocumentJson();
        json[propertyName] = null;
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            SnapshotBlobDocumentSerializer.Deserialize(new(json.ToJsonString())));
        JsonException jsonException = Assert.IsType<JsonException>(exception.InnerException);
        Assert.Equal($"$.{propertyName}", jsonException.Path);
    }

    /// <summary>
    ///     Verifies null caller input is rejected before deserialization.
    /// </summary>
    [Fact]
    public void DeserializeShouldThrowWhenDocumentIsNull() =>
        Assert.Throws<ArgumentNullException>(() => SnapshotBlobDocumentSerializer.Deserialize(null!));

    /// <summary>
    ///     Verifies null caller input is rejected before serialization.
    /// </summary>
    [Fact]
    public void SerializeShouldThrowWhenDocumentIsNull() =>
        Assert.Throws<ArgumentNullException>(() => SnapshotBlobDocumentSerializer.Serialize(null!));

    /// <summary>
    ///     Verifies schema v1 serializes the required document properties with the expected names.
    /// </summary>
    [Fact]
    public void SerializeShouldWriteSchemaVersionOneWithRequiredProperties()
    {
        string payload = Convert.ToBase64String([1, 2, 3, 4]);
        SnapshotBlobDocument document = new()
        {
            SchemaVersion = SnapshotBlobDocument.CurrentSchemaVersion,
            BrookName = "TEST.BROOK",
            SnapshotStorageName = "BankAccountBalance",
            EntityId = "acct-123",
            ReducersHash = "reducers-hash",
            Version = 17,
            DataContentType = "application/octet-stream",
            DataSizeBytes = 4,
            Compression = SnapshotBlobCompression.Gzip,
            StoredSizeBytes = 4,
            Data = payload,
        };
        BinaryData json = SnapshotBlobDocumentSerializer.Serialize(document);
        using JsonDocument parsed = JsonDocument.Parse(json.ToString());
        JsonElement root = parsed.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("TEST.BROOK", root.GetProperty("brookName").GetString());
        Assert.Equal("BankAccountBalance", root.GetProperty("snapshotStorageName").GetString());
        Assert.Equal("acct-123", root.GetProperty("entityId").GetString());
        Assert.Equal("reducers-hash", root.GetProperty("reducersHash").GetString());
        Assert.Equal(17, root.GetProperty("version").GetInt64());
        Assert.Equal("application/octet-stream", root.GetProperty("dataContentType").GetString());
        Assert.Equal(4, root.GetProperty("dataSizeBytes").GetInt64());
        Assert.Equal("gzip", root.GetProperty("compression").GetString());
        Assert.Equal(4, root.GetProperty("storedSizeBytes").GetInt64());
        Assert.Equal(payload, root.GetProperty("data").GetString());
    }
}