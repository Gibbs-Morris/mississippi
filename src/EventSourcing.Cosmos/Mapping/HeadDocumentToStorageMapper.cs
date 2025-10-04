using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Mapping;

/// <summary>
///     Maps head documents to head storage models.
/// </summary>
internal class HeadDocumentToStorageMapper : IMapper<HeadDocument, HeadStorageModel>
{
    /// <summary>
    ///     Maps a head document to a head storage model.
    /// </summary>
    /// <param name="input">The head document to map.</param>
    /// <returns>The mapped head storage model.</returns>
    public HeadStorageModel Map(
        HeadDocument input
    )
    {
        return new()
        {
            Position = new(input.Position),
            OriginalPosition = input.OriginalPosition.HasValue ? new BrookPosition(input.OriginalPosition.Value) : null,
        };
    }
}
