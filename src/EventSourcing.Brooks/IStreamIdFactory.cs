using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Brooks;

/// <summary>
///     Factory interface for creating Orleans stream identifiers from brook keys.
///     Provides a way to map brook keys to Orleans streaming system identifiers.
/// </summary>
public interface IStreamIdFactory
{
    /// <summary>
    ///     Creates an Orleans stream identifier from the specified brook key.
    /// </summary>
    /// <param name="brookKey">The brook key to convert to a stream identifier.</param>
    /// <returns>An Orleans <see cref="StreamId" /> that corresponds to the brook key.</returns>
    StreamId Create(
        BrookKey brookKey
    );
}