namespace Crescent.ConsoleApp;

/// <summary>
///     Represents the cursor information for a specific stream.
/// </summary>
internal sealed class StreamState
{
    /// <summary>
    ///     Gets or sets the last known cursor position for this stream.
    /// </summary>
    public long Cursor { get; set; }

    /// <summary>
    ///     Gets or sets the stream identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the stream type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}