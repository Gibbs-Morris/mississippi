using System;

using Mississippi.EventSourcing.Brooks.Cosmos.Mapping;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Mapping;

/// <summary>
///     Test class for EventDocumentToStorageMapper functionality.
///     Contains unit tests to verify the behavior of event document to storage mapping operations.
/// </summary>
public sealed class EventDocumentToStorageMapperTests
{
    /// <summary>
    ///     Verifies mapping copies fields from document to storage model.
    /// </summary>
    [Fact]
    public void MapCopiesFields()
    {
        EventDocument doc = new()
        {
            EventId = "e1",
            Source = "s",
            EventType = "t",
            DataContentType = "ct",
            Data = new byte[] { 1, 2 },
            DataSizeBytes = 2,
            Time = DateTimeOffset.UtcNow,
        };
        EventDocumentToStorageMapper mapper = new();
        EventStorageModel result = mapper.Map(doc);
        Assert.Equal(doc.EventId, result.EventId);
        Assert.Equal(doc.Source, result.Source);
        Assert.Equal(doc.EventType, result.EventType);
        Assert.Equal(doc.DataContentType, result.DataContentType);
        Assert.Equal(doc.Data, result.Data);
        Assert.Equal(2, result.DataSizeBytes);
        Assert.Equal(doc.Time, result.Time);
    }
}