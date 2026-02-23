using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Storage model for brook cursor position information.
/// </summary>
internal sealed class CursorStorageModel
{
    /// <summary>
    ///     Gets or sets the original position of the brook cursor before any updates.
    /// </summary>
    public BrookPosition? OriginalPosition { get; set; }

    /// <summary>
    ///     Gets or sets the current position of the brook cursor.
    /// </summary>
    public BrookPosition Position { get; set; }
}