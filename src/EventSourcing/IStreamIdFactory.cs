using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing;

public interface IStreamIdFactory
{
    StreamId Create(
        BrookKey brookKey
    );
}