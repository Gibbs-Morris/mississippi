using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Mapping;

internal class HeadDocumentToStorageMapper : IMapper<HeadDocument, HeadStorageModel>
{
    public HeadStorageModel Map(HeadDocument input)
    {
        return new HeadStorageModel
        {
            Position = input.Position,
            OriginalPosition = input.OriginalPosition
        };
    }
}