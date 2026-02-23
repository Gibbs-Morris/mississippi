using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Mapping;

/// <summary>
///     Maps cursor documents to cursor storage models.
/// </summary>
internal sealed class CursorDocumentToStorageMapper : IMapper<CursorDocument, CursorStorageModel>
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