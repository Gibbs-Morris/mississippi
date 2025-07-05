using Mississippi.Core.Streams.Grains.Head;
using Mississippi.Core.Streams.Grains.Reader;
using Mississippi.Core.Streams.Grains.Writer;


namespace Mississippi.Core.Streams.Grains.Factory;

public interface IStreamGrainFactory
{
    IStreamWriterGrain GetStreamWriterGrain(
        StreamCompositeKey streamCompositeKey
    );

    IStreamReaderGrain GetStreamReaderGrain(
        StreamCompositeKey streamCompositeKey
    );

    IStreamSliceReaderGrain GetStreamSliceGrain(
        StreamCompositeRangeKey streamCompositeRangeKey
    );

    IStreamHeadGrain GetStreamHeadGrain(
        StreamCompositeKey streamCompositeKey
    );
}