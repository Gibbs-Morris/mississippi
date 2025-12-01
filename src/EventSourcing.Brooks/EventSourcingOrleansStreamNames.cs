namespace Mississippi.EventSourcing;

/// <summary>
///     Contains constant stream names used for Orleans event sourcing cursor updates.
/// </summary>
public static class EventSourcingOrleansStreamNames
{
    /// <summary>
    ///     The stream name used for brook cursor position update notifications in Orleans.
    ///     This stream carries notifications when brook cursor positions change.
    /// </summary>
    public const string CursorUpdateStreamName = "BrookCursorUpdates";
}