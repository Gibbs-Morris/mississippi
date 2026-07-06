using System;
using System.Text.Json;

using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for Blob snapshot JSON document serialization.
/// </summary>
public sealed class SnapshotBlobDocumentTests
{
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