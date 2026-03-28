using System;

using Azure;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Converts Brooks events to and from Azure Blob event documents.
/// </summary>
internal interface IAzureBrookEventDocumentCodec
{
    /// <summary>
    ///     Decodes a persisted event document into a Brooks event.
    /// </summary>
    /// <param name="payload">The serialized blob payload.</param>
    /// <returns>The decoded Brooks event.</returns>
    BrookEvent Decode(
        BinaryData payload
    );

    /// <summary>
    ///     Encodes a Brooks event for storage at the specified stream position.
    /// </summary>
    /// <param name="brookEvent">The event to encode.</param>
    /// <param name="position">The committed position the payload will represent.</param>
    /// <returns>The serialized blob payload.</returns>
    BinaryData Encode(
        BrookEvent brookEvent,
        long position
    );
}