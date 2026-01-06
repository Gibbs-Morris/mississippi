using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Brooks;

/// <summary>
///     Default implementation of <see cref="IStreamIdFactory" /> that creates Orleans stream identifiers
///     from brook keys using the predefined cursor update stream name.
/// </summary>
internal sealed class StreamIdFactory : IStreamIdFactory
{
    /// <summary>
    ///     Creates an Orleans stream identifier from the specified brook key.
    ///     Uses the cursor update stream name to create a stream identifier for brook cursor position tracking.
    /// </summary>
    /// <param name="brookKey">The brook key to convert to a stream identifier.</param>
    /// <returns>An Orleans <see cref="StreamId" /> for the brook cursor update stream.</returns>
    public StreamId Create(
        BrookKey brookKey
    ) =>
        StreamId.Create(EventSourcingOrleansStreamNames.CursorUpdateStreamName, brookKey);
}