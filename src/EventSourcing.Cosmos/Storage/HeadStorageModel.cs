using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
///     Storage model for brook head position information.
/// </summary>
internal class HeadStorageModel
{
    /// <summary>
    ///     Gets or sets the current position of the brook head.
    /// </summary>
    public BrookPosition Position { get; set; }

    /// <summary>
    ///     Gets or sets the original position of the brook head before any updates.
    /// </summary>
    public BrookPosition? OriginalPosition { get; set; }
}