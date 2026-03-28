using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Brooks;

/// <summary>
///     Appends Brooks event batches using Azure Blob Storage coordination rules.
/// </summary>
internal interface IEventBrookWriter
{
    /// <summary>
    ///     Appends a batch of events to the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="expectedVersion">The expected current cursor position.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    );
}