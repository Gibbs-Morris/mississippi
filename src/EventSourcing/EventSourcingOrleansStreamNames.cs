namespace Mississippi.EventSourcing;

/// <summary>
///     Contains constant stream names used for Orleans event sourcing head updates.
/// </summary>
public static class EventSourcingOrleansStreamNames
{
    /// <summary>
    ///     The stream name used for brook head position update notifications in Orleans.
    ///     This stream carries notifications when brook head positions change.
    /// </summary>
    public const string HeadUpdateStreamName = "StreamHeadUpdates";
}