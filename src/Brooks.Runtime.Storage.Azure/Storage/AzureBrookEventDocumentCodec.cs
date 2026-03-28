using System;
using System.Collections.Immutable;
using System.Linq;

using Azure;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Serializes Brooks events to and from Azure Blob event documents.
/// </summary>
internal sealed class AzureBrookEventDocumentCodec : IAzureBrookEventDocumentCodec
{
    /// <inheritdoc />
    public BrookEvent Decode(
        BinaryData payload
    )
    {
        ArgumentNullException.ThrowIfNull(payload);

        AzureBrookEventDocument document = payload.ToObjectFromJson<AzureBrookEventDocument>() ?? throw new InvalidOperationException(
            "Brooks Azure event blob payload was empty.");

        return new BrookEvent
        {
            Id = document.EventId,
            Source = document.Source,
            EventType = document.EventType,
            DataContentType = document.DataContentType,
            Data = ImmutableArray.Create(document.Data),
            DataSizeBytes = document.DataSizeBytes,
            Time = document.Time,
        };
    }

    /// <inheritdoc />
    public BinaryData Encode(
        BrookEvent brookEvent,
        long position
    )
    {
        ArgumentNullException.ThrowIfNull(brookEvent);

        AzureBrookEventDocument document = new()
        {
            Position = position,
            EventId = brookEvent.Id,
            Source = brookEvent.Source,
            EventType = brookEvent.EventType,
            DataContentType = brookEvent.DataContentType,
            Data = brookEvent.Data.ToArray(),
            DataSizeBytes = brookEvent.DataSizeBytes > 0 ? brookEvent.DataSizeBytes : brookEvent.Data.Length,
            Time = brookEvent.Time,
        };

        return BinaryData.FromObjectAsJson(document);
    }
}