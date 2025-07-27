using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing;

public class StreamIdFactory : IStreamIdFactory
{
    public StreamId Create(
        BrookKey brookKey
    ) =>
        StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, brookKey);
}