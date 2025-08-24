using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Tests.Mapping;

/// <summary>
///     Test class for EventDocumentToStorageMapper functionality.
///     Contains unit tests to verify the behavior of event document to storage mapping operations.
/// </summary>
public class EventDocumentToStorageMapperTests
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
            Time = DateTimeOffset.UtcNow,
        };
        EventDocumentToStorageMapper mapper = new();
        EventStorageModel result = mapper.Map(doc);
        Assert.Equal(doc.EventId, result.EventId);
        Assert.Equal(doc.Source, result.Source);
        Assert.Equal(doc.EventType, result.EventType);
        Assert.Equal(doc.DataContentType, result.DataContentType);
        Assert.Equal(doc.Data, result.Data);
        Assert.Equal(doc.Time, result.Time);
    }
}