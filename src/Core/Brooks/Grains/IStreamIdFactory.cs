using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Brooks.Grains;

public interface IStreamIdFactory
{
    StreamId Create(BrookKey brookKey);
}