using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Mississippi.Tributary.Runtime.Storage.Blob.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Verifies the increment-2 Azure Blob SDK seam directly.
/// </summary>
public sealed class SnapshotBlobOperationsTests
{
    /// <summary>
    ///     Verifies successful creates use the Blob SDK conditional-create wildcard.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateIfAbsentAsyncShouldUseIfNoneMatchWildcardWhenUploadSucceeds()
    {
        const string blobName = "snapshots/hash/v00000000000000000012.snapshot";
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        BlobUploadOptions? capturedOptions = null;
        CancellationToken capturedCancellationToken = default;

        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        blob.Setup(client => client.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, BlobUploadOptions, CancellationToken>((stream, options, cancellationToken) =>
            {
                capturedOptions = options;
                capturedCancellationToken = cancellationToken;
            })
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        SnapshotBlobOperations operations = new(container.Object);
        using MemoryStream content = new([1, 2, 3]);
        using CancellationTokenSource cancellationTokenSource = new();

        bool created = await operations.CreateIfAbsentAsync(blobName, content, cancellationTokenSource.Token);

        Assert.True(created);
        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions.Conditions);
        Assert.Equal(ETag.All, capturedOptions.Conditions.IfNoneMatch);
        Assert.Equal(cancellationTokenSource.Token, capturedCancellationToken);
    }

    /// <summary>
    ///     Verifies duplicate-create Azure responses map to a false result.
    /// </summary>
    /// <param name="statusCode">The Azure response status code.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(409)]
    [InlineData(412)]
    public async Task CreateIfAbsentAsyncShouldReturnFalseForDuplicateResponses(
        int statusCode
    )
    {
        const string blobName = "snapshots/hash/v00000000000000000012.snapshot";
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();

        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        blob.Setup(client => client.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(statusCode, "duplicate"));

        SnapshotBlobOperations operations = new(container.Object);
        using MemoryStream content = new([1, 2, 3]);

        bool created = await operations.CreateIfAbsentAsync(blobName, content, CancellationToken.None);

        Assert.False(created);
    }

    /// <summary>
    ///     Verifies exact-name downloads return the Blob content bytes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadIfExistsAsyncShouldReturnBlobContentWhenTheBlobExists()
    {
        const string blobName = "snapshots/hash/v00000000000000000012.snapshot";
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();

        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        blob.Setup(client => client.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromBytes([1, 2, 3])), Mock.Of<Response>()));

        SnapshotBlobOperations operations = new(container.Object);

        byte[]? content = await operations.DownloadIfExistsAsync(blobName, CancellationToken.None);

        Assert.NotNull(content);
        Assert.Equal([1, 2, 3], content);
    }

    /// <summary>
    ///     Verifies exact-name downloads return null for missing blobs.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadIfExistsAsyncShouldReturnNullWhenTheBlobDoesNotExist()
    {
        const string blobName = "snapshots/hash/v00000000000000000012.snapshot";
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();

        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        blob.Setup(client => client.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "missing"));

        SnapshotBlobOperations operations = new(container.Object);

        byte[]? content = await operations.DownloadIfExistsAsync(blobName, CancellationToken.None);

        Assert.Null(content);
    }

    /// <summary>
    ///     Verifies exact-name deletes forward to the Blob SDK and return the delete result.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteIfExistsAsyncShouldReturnDeleteResultFromTheBlobSdk()
    {
        const string blobName = "snapshots/hash/v00000000000000000012.snapshot";
        Mock<BlobContainerClient> container = new();
        Mock<BlobClient> blob = new();
        CancellationToken capturedCancellationToken = default;

        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        blob.Setup(client => client.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteSnapshotsOption, BlobRequestConditions, CancellationToken>((_, _, cancellationToken) => capturedCancellationToken = cancellationToken)
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        SnapshotBlobOperations operations = new(container.Object);
        using CancellationTokenSource cancellationTokenSource = new();

        bool deleted = await operations.DeleteIfExistsAsync(blobName, cancellationTokenSource.Token);

        Assert.True(deleted);
        Assert.Equal(cancellationTokenSource.Token, capturedCancellationToken);
    }

    /// <summary>
    ///     Verifies prefix listing forwards the stream prefix and requested page size into Azure paging.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListByPrefixAsyncShouldForwardPrefixAndPageSizeHint()
    {
        const string prefix = "snapshots/hash/";
        const int pageSizeHint = 17;
        string? capturedPrefix = null;
        CancellationToken capturedCancellationToken = default;
        RecordingAsyncPageable pageable = new(
            CreatePage(
                [
                    CreateBlobItem("snapshots/hash/v00000000000000000009.snapshot"),
                    CreateBlobItem("snapshots/hash/v00000000000000000010.snapshot"),
                ],
                "token-1"),
            CreatePage(
                [
                    CreateBlobItem("snapshots/hash/v00000000000000000011.snapshot"),
                ],
                null));
        Mock<BlobContainerClient> container = new();

        container.Setup(client => client.GetBlobsAsync(BlobTraits.None, BlobStates.None, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<BlobTraits, BlobStates, string, CancellationToken>((traits, states, requestedPrefix, cancellationToken) =>
            {
                Assert.Equal(BlobTraits.None, traits);
                Assert.Equal(BlobStates.None, states);
                capturedPrefix = requestedPrefix;
                capturedCancellationToken = cancellationToken;
            })
            .Returns(pageable);

        SnapshotBlobOperations operations = new(container.Object);
        using CancellationTokenSource cancellationTokenSource = new();
        List<SnapshotBlobPage> pages = [];

        await foreach (SnapshotBlobPage page in operations.ListByPrefixAsync(prefix, pageSizeHint, cancellationTokenSource.Token))
        {
            pages.Add(page);
        }

        Assert.Equal(prefix, capturedPrefix);
        Assert.Equal(cancellationTokenSource.Token, capturedCancellationToken);
        Assert.Null(pageable.ContinuationToken);
        Assert.Equal(pageSizeHint, pageable.PageSizeHint);
        Assert.Collection(
            pages,
            page => Assert.Equal(
                ["snapshots/hash/v00000000000000000009.snapshot", "snapshots/hash/v00000000000000000010.snapshot"],
                page.BlobNames),
            page => Assert.Equal(["snapshots/hash/v00000000000000000011.snapshot"], page.BlobNames));
    }

    private static BlobItem CreateBlobItem(
        string name
    ) =>
        BlobsModelFactory.BlobItem(name, false, null!, null, new Dictionary<string, string>());

    private static Page<BlobItem> CreatePage(
        IReadOnlyList<BlobItem> items,
        string? continuationToken
    ) =>
        Page<BlobItem>.FromValues(items, continuationToken, Mock.Of<Response>());

    private sealed class RecordingAsyncPageable(
        params Page<BlobItem>[] pages
    ) : AsyncPageable<BlobItem>
    {
        public string? ContinuationToken { get; private set; }

        public int? PageSizeHint { get; private set; }

        public override IAsyncEnumerable<Page<BlobItem>> AsPages(
            string? continuationToken = null,
            int? pageSizeHint = null
        )
        {
            ContinuationToken = continuationToken;
            PageSizeHint = pageSizeHint;
            return EnumeratePagesAsync();
        }

        private async IAsyncEnumerable<Page<BlobItem>> EnumeratePagesAsync()
        {
            foreach (Page<BlobItem> page in pages)
            {
                yield return page;
                await Task.CompletedTask;
            }
        }
    }
}