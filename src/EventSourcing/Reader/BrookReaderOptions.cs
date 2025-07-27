namespace Mississippi.EventSourcing.Reader;

public class BrookReaderOptions
{
    public long BrookSliceSize { get; init; } = 100;
}