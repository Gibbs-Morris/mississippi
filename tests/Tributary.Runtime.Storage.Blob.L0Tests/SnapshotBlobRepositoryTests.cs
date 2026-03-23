using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Verifies increment-2 repository primitives built on Blob naming and listing.
/// </summary>
public sealed class SnapshotBlobRepositoryTests
{
    /// <summary>
    ///     Verifies conditional create succeeds for a new Blob name.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteIfAbsentAsyncShouldUseConditionalCreateAndSucceedWhenBlobIsNew()
    {
        StubSnapshotBlobOperations operations = new()
        {
            CreateIfAbsentResult = true,
        };
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        using MemoryStream content = new([1, 2, 3]);

        await repository.WriteIfAbsentAsync(snapshotKey, content, CancellationToken.None);

        Assert.Single(operations.CreatedBlobNames);
        Assert.EndsWith("v00000000000000000012.snapshot", operations.CreatedBlobNames[0], System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies duplicate writes surface the internal duplicate-version conflict.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteIfAbsentAsyncShouldThrowDuplicateVersionConflictWhenBlobAlreadyExists()
    {
        StubSnapshotBlobOperations operations = new()
        {
            CreateIfAbsentResult = false,
        };
        SnapshotBlobRepository repository = CreateRepository(operations);
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 12);
        using MemoryStream content = new([1, 2, 3]);

        SnapshotBlobDuplicateVersionException exception = await Assert.ThrowsAsync<SnapshotBlobDuplicateVersionException>(
            () => repository.WriteIfAbsentAsync(snapshotKey, content, CancellationToken.None));

        Assert.Equal(snapshotKey, exception.SnapshotKey);
    }

    /// <summary>
    ///     Verifies stream-local listing only yields versions for the requested stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListVersionsAsyncShouldReturnOnlyVersionsForTheRequestedStream()
    {
        SnapshotStreamKey targetStream = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy nameStrategy = CreateNameStrategy();
        StubSnapshotBlobOperations operations = new();
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(targetStream, 9)),
            nameStrategy.GetBlobName(new(targetStream, 10)),
            nameStrategy.GetBlobName(new(new SnapshotStreamKey("brook-a", "projection-a", "entity-43", "reducers-v1"), 999)),
            "not-a-snapshot-name",
        ]));

        SnapshotBlobRepository repository = CreateRepository(operations, nameStrategy);

        List<IReadOnlyList<long>> pages = [];
        await foreach (IReadOnlyList<long> page in repository.ListVersionsAsync(targetStream, CancellationToken.None))
        {
            pages.Add(page);
        }

        Assert.Single(pages);
        Assert.Equal([9L, 10L], pages[0]);
        Assert.Single(operations.ListPrefixes);
        Assert.Equal(nameStrategy.GetStreamPrefix(targetStream), operations.ListPrefixes[0]);
        Assert.Single(operations.ListPageSizeHints);
        Assert.Equal(SnapshotBlobDefaults.ListPageSizeHint, operations.ListPageSizeHints[0]);
    }

    /// <summary>
    ///     Verifies latest-version selection uses parsed names across multiple pages.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetLatestVersionAsyncShouldSelectHighestVersionAcrossPages()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy nameStrategy = CreateNameStrategy();
        StubSnapshotBlobOperations operations = new();
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(streamKey, 9)),
            nameStrategy.GetBlobName(new(streamKey, 10)),
        ]));
        operations.Pages.Add(new SnapshotBlobPage([
            nameStrategy.GetBlobName(new(streamKey, 11)),
            nameStrategy.GetBlobName(new(streamKey, 100)),
        ]));

        SnapshotBlobRepository repository = CreateRepository(operations, nameStrategy);

        long? latestVersion = await repository.GetLatestVersionAsync(streamKey, CancellationToken.None);

        Assert.Equal(100, latestVersion);
    }

    private static BlobNameStrategy CreateNameStrategy() =>
        new(Options.Create(new SnapshotBlobStorageOptions()));

    private static SnapshotBlobRepository CreateRepository(
        StubSnapshotBlobOperations operations,
        BlobNameStrategy? nameStrategy = null
    ) =>
        new(nameStrategy ?? CreateNameStrategy(), operations, Options.Create(new SnapshotBlobStorageOptions()));
}