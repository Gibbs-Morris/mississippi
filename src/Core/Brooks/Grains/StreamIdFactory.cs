using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Brooks.Grains;

public class StreamIdFactory : IStreamIdFactory
{
    public StreamId Create(BrookKey brookKey)
    {
        return StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, brookKey);
    }
}