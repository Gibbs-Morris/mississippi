using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Tests.Mapping;

/// <summary>
///     Test class for <see cref="Mississippi.EventSourcing.Cosmos.Mapping.CursorDocumentToStorageMapper" /> functionality.
///     Contains unit tests to verify the behavior of head document to storage mapping operations.
/// </summary>
public class CursorDocumentToStorageMapperTests
{
    /// <summary>
    ///     Verifies that when OriginalPosition is present both values are populated.
    /// </summary>
    [Fact]
    public void MapWithOriginalPositionPopulatesBoth()
    {
        // Arrange
        CursorDocument doc = new()
        {
            Position = 42L,
            OriginalPosition = 10L,
        };
        CursorDocumentToStorageMapper mapper = new();

        // Act
        CursorStorageModel result = mapper.Map(doc);

        // Assert
        Assert.Equal(42L, (long)result.Position);
        Assert.NotNull(result.OriginalPosition);
        Assert.Equal(10L, (long)result.OriginalPosition.Value);
    }

    /// <summary>
    ///     Verifies that when OriginalPosition is null the mapped OriginalPosition is null.
    /// </summary>
    [Fact]
    public void MapWithoutOriginalPositionSetsNullOriginal()
    {
        // Arrange
        CursorDocument doc = new()
        {
            Position = 5L,
            OriginalPosition = null,
        };
        CursorDocumentToStorageMapper mapper = new();

        // Act
        CursorStorageModel result = mapper.Map(doc);

        // Assert
        Assert.Equal(5L, (long)result.Position);
        Assert.Null(result.OriginalPosition);
    }
}