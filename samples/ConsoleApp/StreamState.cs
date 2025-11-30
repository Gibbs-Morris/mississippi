namespace Crescent.ConsoleApp;

/// <summary>
///     Represents the head information for a specific stream.
/// </summary>
internal sealed class StreamState
{
    /// <summary>
    ///     Gets or sets the last known head position for this stream.
    /// </summary>
    public long Head { get; set; }

    /// <summary>
    ///     Gets or sets the stream identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the stream type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}