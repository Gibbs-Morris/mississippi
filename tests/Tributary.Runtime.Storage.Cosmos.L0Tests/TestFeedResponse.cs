using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Azure.Cosmos;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Provides a deterministic typed Cosmos feed response for unit tests.
/// </summary>
/// <typeparam name="T">The item type returned by the response.</typeparam>
internal sealed class TestFeedResponse<T> : FeedResponse<T>
{
    private readonly IReadOnlyList<T> items;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFeedResponse{T}" /> class.
    /// </summary>
    /// <param name="items">The items in the response page.</param>
    internal TestFeedResponse(
        IReadOnlyList<T> items
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        this.items = items;
    }

    /// <inheritdoc />
    public override string ContinuationToken => string.Empty;

    /// <inheritdoc />
    public override int Count => items.Count;

    /// <inheritdoc />
    public override CosmosDiagnostics Diagnostics => null!;

    /// <inheritdoc />
    public override Headers Headers => new();

    /// <inheritdoc />
    public override string IndexMetrics => string.Empty;

    /// <inheritdoc />
    public override IEnumerable<T> Resource => items;

    /// <inheritdoc />
    public override HttpStatusCode StatusCode => HttpStatusCode.OK;

    /// <inheritdoc />
    public override IEnumerator<T> GetEnumerator() => items.GetEnumerator();
}