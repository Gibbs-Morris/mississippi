using Mississippi.EventSourcing.Abstractions.Brooks;


namespace Mississippi.EventSourcing.Brooks.Grains;

public interface IStreamIdFactory
{
    StreamId Create(
        BrookKey brookKey
    );
}