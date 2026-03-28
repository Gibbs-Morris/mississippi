using System.Text.Json.Serialization;

using Azure;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Represents the committed Brooks cursor document stored in Azure Blob Storage.
/// </summary>
internal sealed record AzureBrookCommittedCursorState
{
    /// <summary>
    ///     Gets the blob ETag used for conditional updates.
    /// </summary>
    [JsonIgnore]
    public ETag ETag { get; init; }

    /// <summary>
    ///     Gets the committed event position.
    /// </summary>
    public long Position { get; init; }

    /// <summary>
    ///     Gets the persisted document schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;
}