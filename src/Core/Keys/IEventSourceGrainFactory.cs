using Mississippi.Core.Idea.StreamGrains;


namespace Mississippi.Core.Keys;

public interface IEventSourceGrainFactory
{
    IStreamWriterGrain GetStreamWriterGrain(
        StreamGrainKey streamId
    );

    IStreamReaderGrain GetStreamGrain(
        StreamGrainKey streamId
    );

    IStreamSliceReaderGrain GetStreamSliceGrain(
        StreamSliceGrainKey streamSliceId
    );

    IStreamHeadGrain GetStreamHeadGrain(
        StreamGrainKey streamId
    );
}