namespace Mississippi.EventSourcing.Brooks.Grains.Reader;

public class BrookReaderOptions
{
    public long BrookSliceSize { get; init; } = 100;
}