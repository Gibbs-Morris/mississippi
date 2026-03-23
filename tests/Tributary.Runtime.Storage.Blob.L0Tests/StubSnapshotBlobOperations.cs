using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    ///     Gets the stored Blob payloads by Blob name.
    /// </summary>
    public Dictionary<string, byte[]> Blobs { get; } = new(System.StringComparer.Ordinal);

    /// <summary>
    ///     Gets the Blob names captured for conditional-create calls.
    /// </summary>
    public List<string> CreatedBlobNames { get; } = [];

    /// <summary>
    ///     Gets a value indicating whether conditional create should report success.
    /// </summary>
    public bool CreateIfAbsentResult { get; init; } = true;

    /// <summary>
    ///     Gets the Blob names captured for delete calls.
    /// </summary>
    public List<string> DeletedBlobNames { get; } = [];

    /// <summary>
    ///     Gets the Blob names captured for download calls.
    /// </summary>
    public List<string> DownloadedBlobNames { get; } = [];

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
    public async Task<bool> CreateIfAbsentAsync(
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        CreatedBlobNames.Add(blobName);

        if (!CreateIfAbsentResult || Blobs.ContainsKey(blobName))
        {
            return false;
        }

        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        Blobs[blobName] = buffer.ToArray();
        return true;
    }

    /// <inheritdoc />
    public Task<bool> DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        DeletedBlobNames.Add(blobName);
        return Task.FromResult(Blobs.Remove(blobName));
    }

    /// <inheritdoc />
    public Task<byte[]?> DownloadIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        DownloadedBlobNames.Add(blobName);
        return Task.FromResult<byte[]?>(Blobs.TryGetValue(blobName, out byte[]? content) ? [.. content] : null);
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

        IEnumerable<SnapshotBlobPage> pages = Pages.Count > 0
            ? Pages
            : CreatePagesFromStoredBlobs(prefix, pageSizeHint);

        foreach (SnapshotBlobPage page in pages)
        {
            yield return page;
            await Task.Yield();
        }
    }

    private IEnumerable<SnapshotBlobPage> CreatePagesFromStoredBlobs(
        string prefix,
        int pageSizeHint
    )
    {
        string[] matchingBlobNames = Blobs.Keys
            .Where(blobName => blobName.StartsWith(prefix, System.StringComparison.Ordinal))
            .OrderBy(blobName => blobName, System.StringComparer.Ordinal)
            .ToArray();

        for (int index = 0; index < matchingBlobNames.Length; index += pageSizeHint)
        {
            yield return new SnapshotBlobPage(matchingBlobNames.Skip(index).Take(pageSizeHint).ToArray());
        }
    }
}