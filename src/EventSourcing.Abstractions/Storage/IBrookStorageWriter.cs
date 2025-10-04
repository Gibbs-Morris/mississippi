namespace Mississippi.EventSourcing.Abstractions.Storage;

/// <summary>
///     Provides write access to brooks.
/// </summary>
public interface IBrookStorageWriter
{
    /// <summary>
    ///     Appends events to a brook.
    /// </summary>
    /// <param name="brookId">The identifier of the brook.</param>
    /// <param name="events">The events to append to the brook.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The new head position after appending the events.</returns>
    Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    );
}
