using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.Core.Brooks.Grains;

public class StreamIdFactory : IStreamIdFactory
{
    public StreamId Create(
        BrookKey brookKey
    ) =>
        StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, brookKey);
}