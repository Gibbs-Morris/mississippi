using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Mapping;

/// <summary>
///     Maps cursor documents to cursor storage models.
/// </summary>
internal class CursorDocumentToStorageMapper : IMapper<CursorDocument, CursorStorageModel>
{
    /// <summary>
    ///     Maps a cursor document to a cursor storage model.
    /// </summary>
    /// <param name="input">The cursor document to map.</param>
    /// <returns>The mapped cursor storage model.</returns>
    public CursorStorageModel Map(
        CursorDocument input
    ) =>
        new()
        {
            Position = new(input.Position),
            OriginalPosition = input.OriginalPosition.HasValue ? new BrookPosition(input.OriginalPosition.Value) : null,
        };
}