using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing;

/// <summary>
///     Default implementation of <see cref="IStreamIdFactory" /> that creates Orleans stream identifiers
///     from brook keys using the predefined head update stream name.
/// </summary>
public class StreamIdFactory : IStreamIdFactory
{
    /// <summary>
    ///     Creates an Orleans stream identifier from the specified brook key.
    ///     Uses the head update stream name to create a stream identifier for brook head position tracking.
    /// </summary>
    /// <param name="brookKey">The brook key to convert to a stream identifier.</param>
    /// <returns>An Orleans <see cref="StreamId" /> for the brook head update stream.</returns>
    public StreamId Create(
        BrookKey brookKey
    ) =>
        StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, brookKey);
}
