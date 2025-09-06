namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Configuration options for brook reader operations.
///     Contains settings that control how brooks are read and processed.
/// </summary>
public class BrookReaderOptions
{
    /// <summary>
    ///     Gets or initializes the number of events to read in each brook slice operation.
    ///     Default value is 100. Controls the batch size for reading events from brooks.
    /// </summary>
    /// <value>The number of events per brook slice.</value>
    public long BrookSliceSize { get; init; } = 100;
}