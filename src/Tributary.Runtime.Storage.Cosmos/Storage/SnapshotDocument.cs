using System;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Cosmos document representation of a snapshot.
/// </summary>
internal sealed class SnapshotDocument
{
    /// <summary>
    ///     Gets or sets the brook name identifying the event stream.
    /// </summary>
    [JsonProperty("brookName")]
    public string BrookName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the snapshot payload bytes.
    /// </summary>
    [JsonProperty("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    ///     Gets or sets the MIME type describing the data.
    /// </summary>
    [JsonProperty("dataContentType")]
    public string DataContentType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the size of the snapshot payload in bytes.
    /// </summary>
    /// <remarks>
    ///     This denormalized field enables efficient Cosmos DB queries for large snapshots.
    /// </remarks>
    [JsonProperty("dataSizeBytes")]
    public long DataSizeBytes { get; set; }

    /// <summary>
    ///     Gets or sets the document identifier (snapshot version).
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the projection identifier associated with the snapshot.
    /// </summary>
    [JsonProperty("projectionId")]
    public string ProjectionId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the projection type associated with the snapshot.
    /// </summary>
    [JsonProperty("projectionType")]
    public string ProjectionType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the reducers hash for the snapshot stream.
    /// </summary>
    [JsonProperty("reducersHash")]
    public string ReducersHash { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the partition key value for the snapshot stream.
    /// </summary>
    [JsonProperty("snapshotPartitionKey")]
    public string SnapshotPartitionKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the discriminator for snapshot documents.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "snapshot";

    /// <summary>
    ///     Gets or sets the snapshot version.
    /// </summary>
    [JsonProperty("version")]
    public long Version { get; set; }
}