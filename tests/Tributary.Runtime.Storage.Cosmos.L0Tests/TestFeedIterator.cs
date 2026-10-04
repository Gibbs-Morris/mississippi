using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Provides deterministic typed Cosmos pages for unit tests.
/// </summary>
/// <typeparam name="T">The item type returned by the iterator.</typeparam>
internal sealed class TestFeedIterator<T> : FeedIterator<T>
{
    private readonly Queue<List<T>> pages;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFeedIterator{T}" /> class.
    /// </summary>
    /// <param name="pages">The pages returned in order.</param>
    internal TestFeedIterator(
        IEnumerable<List<T>> pages
    )
    {
        ArgumentNullException.ThrowIfNull(pages);
        this.pages = new(pages);
    }

    /// <inheritdoc />
    public override bool HasMoreResults => pages.Count > 0;

    /// <inheritdoc />
    public override Task<FeedResponse<T>> ReadNextAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult<FeedResponse<T>>(new TestFeedResponse<T>(pages.Dequeue()));
}