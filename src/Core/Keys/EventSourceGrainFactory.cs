using Mississippi.Core.Idea.StreamGrains;


namespace Mississippi.Core.Keys;

public class EventSourceGrainFactory : IEventSourceGrainFactory
{
    private IGrainFactory GrainFactory { get; }

    public IStreamWriterGrain GetStreamWriterGrain(
        StreamGrainKey streamId
    )
    {
        ArgumentNullException.ThrowIfNull(streamId);
        return GrainFactory.GetGrain<IStreamWriterGrain>(streamId.ToOrleansKey());
    }

    public IStreamReaderGrain GetStreamGrain(
        StreamGrainKey streamId
    )
    {
        ArgumentNullException.ThrowIfNull(streamId);
        return GrainFactory.GetGrain<IStreamReaderGrain>(streamId.ToOrleansKey());
    }

    public IStreamSliceReaderGrain GetStreamSliceGrain(
        StreamSliceGrainKey streamSliceId
    )
    {
        ArgumentNullException.ThrowIfNull(streamSliceId);
        return GrainFactory.GetGrain<IStreamSliceReaderGrain>(streamSliceId.ToOrleansKey());
    }

    public IStreamHeadGrain GetStreamHeadGrain(
        StreamGrainKey streamId
    )
    {
        ArgumentNullException.ThrowIfNull(streamId);
        return GrainFactory.GetGrain<IStreamHeadGrain>(streamId.ToOrleansKey());
    }
}