using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Cosmos;

/// <summary>
///     Provides functionality for appending events to Cosmos DB event brooks.
/// </summary>
internal interface IEventBrookAppender
{
    /// <summary>
    ///     Appends a collection of events to the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="events">The collection of events to append to the brook.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The position after successfully appending all events.</returns>
    Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    );
}