using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Test double for the increment-2 Blob operations seam.
/// </summary>
internal sealed class StubSnapshotBlobOperations : ISnapshotBlobOperations
{
    /// <summary>
    ///     Gets the Blob names captured for conditional-create calls.
    /// </summary>
    public List<string> CreatedBlobNames { get; } = [];

    /// <summary>
    ///     Gets a value indicating whether conditional create should report success.
    /// </summary>
    public bool CreateIfAbsentResult { get; init; } = true;

    /// <summary>
    ///     Gets the page size hints captured for listing calls.
    /// </summary>
    public List<int> ListPageSizeHints { get; } = [];

    /// <summary>
    ///     Gets the prefixes captured for listing calls.
    /// </summary>
    public List<string> ListPrefixes { get; } = [];

    /// <summary>
    ///     Gets the pages that the test double will yield to callers.
    /// </summary>
    public List<SnapshotBlobPage> Pages { get; } = [];

    /// <inheritdoc />
    public Task<bool> CreateIfAbsentAsync(
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        CreatedBlobNames.Add(blobName);
        return Task.FromResult(CreateIfAbsentResult);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SnapshotBlobPage> ListByPrefixAsync(
        string prefix,
        int pageSizeHint,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default
    )
    {
        ListPrefixes.Add(prefix);
        ListPageSizeHints.Add(pageSizeHint);

        foreach (SnapshotBlobPage page in Pages)
        {
            yield return page;
            await Task.Yield();
        }
    }
}