using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.Core.Brooks.Grains;

public interface IStreamIdFactory
{
    StreamId Create(
        BrookKey brookKey
    );
}