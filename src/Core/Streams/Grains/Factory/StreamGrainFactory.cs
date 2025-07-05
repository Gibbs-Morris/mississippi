using Microsoft.Extensions.Logging;
using Mississippi.Core.Streams.Grains.Head;
using Mississippi.Core.Streams.Grains.Reader;
using Mississippi.Core.Streams.Grains.Writer;


namespace Mississippi.Core.Streams.Grains.Factory;

public class StreamGrainFactory : IStreamGrainFactory
{
    public StreamGrainFactory(
        IGrainFactory grainFactory,
        ILogger<StreamGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }
    private ILogger<StreamGrainFactory> Logger { get; }

    public IStreamWriterGrain GetStreamWriterGrain(
        StreamCompositeKey streamCompositeKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {StreamCompositeKey}", nameof(IStreamWriterGrain),
            streamCompositeKey);
        return GrainFactory.GetGrain<IStreamWriterGrain>(streamCompositeKey);
    }

    public IStreamReaderGrain GetStreamReaderGrain(
        StreamCompositeKey streamCompositeKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {StreamCompositeKey}", nameof(IStreamReaderGrain),
            streamCompositeKey);
        return GrainFactory.GetGrain<IStreamReaderGrain>(streamCompositeKey);
    }

    public IStreamSliceReaderGrain GetStreamSliceGrain(
        StreamCompositeRangeKey streamCompositeRangeKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {StreamCompositeRangeKey}", nameof(IStreamSliceReaderGrain),
            streamCompositeRangeKey);
        return GrainFactory.GetGrain<IStreamSliceReaderGrain>(streamCompositeRangeKey);
    }

    public IStreamHeadGrain GetStreamHeadGrain(
        StreamCompositeKey streamCompositeKey
    )
    {
        Logger.LogDebug("Resolving {GrainType} for stream {StreamCompositeKey}", nameof(IStreamHeadGrain),
            streamCompositeKey);
        return GrainFactory.GetGrain<IStreamHeadGrain>(streamCompositeKey);
    }
}