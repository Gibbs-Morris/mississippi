using System;
using System.Collections.Generic;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents a bounded page of durable delivery-state snapshots.
/// </summary>
public sealed class ReplicaSinkDeliveryStatePage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeliveryStatePage" /> class.
    /// </summary>
    /// <param name="items">The durable delivery-state snapshots in the current page.</param>
    /// <param name="continuationToken">The opaque continuation token for the next page, when more items exist.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items" /> is null.</exception>
    public ReplicaSinkDeliveryStatePage(
        IReadOnlyList<ReplicaSinkDeliveryState> items,
        string? continuationToken = null
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
        ContinuationToken = continuationToken;
    }

    /// <summary>
    ///     Gets the opaque continuation token for the next page, when more items exist.
    /// </summary>
    public string? ContinuationToken { get; }

    /// <summary>
    ///     Gets the durable delivery-state snapshots in the current page.
    /// </summary>
    public IReadOnlyList<ReplicaSinkDeliveryState> Items { get; }
}