using Mississippi.EventSourcing.Brooks.Cosmos.Mapping;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Mapping;

/// <summary>
///     Tests <see cref="CursorDocumentToStorageMapper" /> to ensure cursor documents are mapped correctly.
/// </summary>
public sealed class CursorDocumentToStorageMapperTests
{
    /// <summary>
    ///     Verifies the mapper copies both the cursor position and original position when present.
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
    ///     Verifies the mapper leaves OriginalPosition null when the document omits it.
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