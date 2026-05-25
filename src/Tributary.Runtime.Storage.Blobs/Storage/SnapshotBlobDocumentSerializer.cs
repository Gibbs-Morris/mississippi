using System;
using System.IO;
using System.Text.Json;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Serializes and deserializes Blob snapshot JSON documents.
/// </summary>
internal static class SnapshotBlobDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    ///     Deserializes a Blob snapshot document.
    /// </summary>
    /// <param name="document">The serialized JSON document.</param>
    /// <returns>The deserialized document.</returns>
    public static SnapshotBlobDocument Deserialize(
        BinaryData document
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        try
        {
            SnapshotBlobDocument? result =
                JsonSerializer.Deserialize<SnapshotBlobDocument>(document.ToMemory().Span, SerializerOptions);
            if (result is null)
            {
                throw new InvalidDataException("Snapshot Blob document JSON deserialized to null.");
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Snapshot Blob document JSON is invalid.", exception);
        }
    }

    /// <summary>
    ///     Serializes a Blob snapshot document.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <returns>The serialized JSON document.</returns>
    public static BinaryData Serialize(
        SnapshotBlobDocument document
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        return BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(document, SerializerOptions));
    }
}