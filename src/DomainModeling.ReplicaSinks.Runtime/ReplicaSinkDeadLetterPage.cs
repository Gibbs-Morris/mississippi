using System;
using System.Collections.Generic;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents a bounded page of dead-letter records exposed by the runtime operator surface.
/// </summary>
public sealed class ReplicaSinkDeadLetterPage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeadLetterPage" /> class.
    /// </summary>
    /// <param name="items">The dead-letter records in the current page.</param>
    /// <param name="continuationToken">The opaque continuation token for the next page, when more records exist.</param>
    public ReplicaSinkDeadLetterPage(
        IReadOnlyList<ReplicaSinkDeadLetterRecord> items,
        string? continuationToken = null
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
        ContinuationToken = continuationToken;
    }

    /// <summary>
    ///     Gets the opaque continuation token for the next page, when more records exist.
    /// </summary>
    public string? ContinuationToken { get; }

    /// <summary>
    ///     Gets the dead-letter records in the current page.
    /// </summary>
    public IReadOnlyList<ReplicaSinkDeadLetterRecord> Items { get; }
}
