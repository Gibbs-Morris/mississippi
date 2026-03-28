using System;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Represents a serialized Brooks event document stored in Azure Blob Storage.
/// </summary>
internal sealed record AzureBrookEventDocument
{
    /// <summary>
    ///     Gets the event payload content type.
    /// </summary>
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the serialized event payload bytes.
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     Gets the size of the payload in bytes.
    /// </summary>
    public long DataSizeBytes { get; init; }

    /// <summary>
    ///     Gets the logical event identifier.
    /// </summary>
    public string EventId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the event type name.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the committed stream position represented by this blob.
    /// </summary>
    public long Position { get; init; }

    /// <summary>
    ///     Gets the persisted document schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    ///     Gets the event source identifier.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the optional event timestamp.
    /// </summary>
    public DateTimeOffset? Time { get; init; }
}