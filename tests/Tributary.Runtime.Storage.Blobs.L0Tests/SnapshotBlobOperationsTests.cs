using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Blobs;
using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

using Moq;
using Moq.Protected;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests the Azure SDK boundary without network access.
/// </summary>
public sealed class SnapshotBlobOperationsTests
{
    private static Mock<BlobContainerClient> CreateDownloadContainer(
        BlobDownloadStreamingResult download,
        CancellationToken cancellationToken = default
    )
    {
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.DownloadStreamingAsync(null, cancellationToken))
            .ReturnsAsync(Response.FromValue(download, Mock.Of<Response>()));
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient("snapshot.json")).Returns(blob.Object);
        return container;
    }

    private static BlobDownloadStreamingResult CreateDownloadResult(
        Stream content,
        long? declaredLength
    )
    {
        BlobDownloadDetails? details = declaredLength is { } length
            ? BlobsModelFactory.BlobDownloadDetails(contentLength: length)
            : null;
        return BlobsModelFactory.BlobDownloadStreamingResult(content, details);
    }

    /// <summary>
    ///     Verifies a container client is required.
    /// </summary>
    [Fact]
    public void ConstructorShouldRejectNullContainerClient()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new SnapshotBlobOperations(null!, Options.Create(new SnapshotBlobStorageOptions())));
        Assert.Equal("blobContainerClient", exception.ParamName);
    }

    /// <summary>
    ///     Verifies size-limit options are required.
    /// </summary>
    [Fact]
    public void ConstructorShouldRejectNullOptions()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new SnapshotBlobOperations(Mock.Of<BlobContainerClient>(), null!));
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>
    ///     Verifies container initialization keeps snapshots private and forwards cancellation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateContainerIfNotExistsAsyncShouldCreatePrivateContainer()
    {
        using CancellationTokenSource cancellation = new();
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellation.Token))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        await operations.CreateContainerIfNotExistsAsync(cancellation.Token);
        container.Verify(
            client => client.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellation.Token),
            Times.Once);
    }

    /// <summary>
    ///     Verifies delete results distinguish removed and already missing blobs.
    /// </summary>
    /// <param name="wasDeleted">Whether the SDK reports a deletion.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteIfExistsAsyncShouldReturnSdkResult(
        bool wasDeleted
    )
    {
        const string blobName = "stream/0000000000000000001.json";
        using CancellationTokenSource cancellation = new();
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.DeleteBlobIfExistsAsync(blobName, default, null, cancellation.Token))
            .ReturnsAsync(Response.FromValue(wasDeleted, Mock.Of<Response>()));
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        bool result = await operations.DeleteIfExistsAsync(blobName, cancellation.Token);
        Assert.Equal(wasDeleted, result);
        container.Verify(
            client => client.DeleteBlobIfExistsAsync(blobName, default, null, cancellation.Token),
            Times.Once);
    }

    /// <summary>
    ///     Verifies documents at the configured byte limit are accepted, including without length metadata.
    /// </summary>
    /// <param name="declaredLength">The reported document length, or no metadata.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(4L)]
    public async Task DownloadAsyncShouldAcceptExactDocumentSizeLimit(
        long? declaredLength
    )
    {
        byte[] expected = [1, 2, 3, 4];
        using MemoryStream content = new(expected);
        using BlobDownloadStreamingResult download = CreateDownloadResult(content, declaredLength);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download);
        SnapshotBlobOperations operations = new(
            container.Object,
            Options.Create(
                new SnapshotBlobStorageOptions
                {
                    MaximumSnapshotDocumentSizeBytes = expected.Length,
                }));
        BinaryData? result = await operations.DownloadAsync("snapshot.json");
        Assert.NotNull(result);
        Assert.Equal(expected, result.ToArray());
        Assert.False(content.CanRead);
    }

    /// <summary>
    ///     Verifies download cancellation is not converted into a missing snapshot.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldPropagateCancellation()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.DownloadStreamingAsync(null, cancellation.Token))
            .Returns(Task.FromCanceled<Response<BlobDownloadStreamingResult>>(cancellation.Token));
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient("snapshot.json")).Returns(blob.Object);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operations.DownloadAsync("snapshot.json", cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    /// <summary>
    ///     Verifies cancellation reaches stream reads and still releases the downloaded stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldPropagateReadCancellationAndDisposeStream()
    {
        CancellationToken cancellationToken = new(true);
        Mock<Stream> content = new(MockBehavior.Strict);
        content.Setup(stream => stream.Close()).CallBase();
        content.Setup(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), cancellationToken))
            .Returns(() => ValueTask.FromCanceled<int>(cancellationToken));
        content.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        using BlobDownloadStreamingResult download = CreateDownloadResult(content.Object, 0);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download, cancellationToken);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operations.DownloadAsync("snapshot.json", cancellationToken));
        Assert.Equal(cancellationToken, exception.CancellationToken);
        content.Verify(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), cancellationToken), Times.Once);
        content.Protected().Verify("Dispose", Times.Once(), ItExpr.Is<bool>(disposing => disposing));
    }

    /// <summary>
    ///     Verifies stream failures are propagated and the downloaded stream is released.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldPropagateReadFailureAndDisposeStream()
    {
        IOException failure = new("The response stream failed.");
        Mock<Stream> content = new(MockBehavior.Strict);
        content.Setup(stream => stream.Close()).CallBase();
        content.Setup(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
            .Returns(() => ValueTask.FromException<int>(failure));
        content.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        using BlobDownloadStreamingResult download = CreateDownloadResult(content.Object, 0);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        IOException exception = await Assert.ThrowsAsync<IOException>(() => operations.DownloadAsync("snapshot.json"));
        Assert.Same(failure, exception);
        content.Protected().Verify("Dispose", Times.Once(), ItExpr.Is<bool>(disposing => disposing));
    }

    /// <summary>
    ///     Verifies authorization, throttling, and service failures are not mistaken for a missing snapshot.
    /// </summary>
    /// <param name="status">The storage failure status.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(403)]
    [InlineData(409)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task DownloadAsyncShouldPropagateStorageFailures(
        int status
    )
    {
        RequestFailedException failure = new(status, "Storage request failed.");
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.DownloadStreamingAsync(null, CancellationToken.None)).ThrowsAsync(failure);
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient("snapshot.json")).Returns(blob.Object);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        RequestFailedException exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            operations.DownloadAsync("snapshot.json"));
        Assert.Same(failure, exception);
    }

    /// <summary>
    ///     Verifies all chunks of a permitted document are read without truncation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldReadMultipleChunks()
    {
        byte[] expected = new byte[90000];
        expected[0] = 1;
        expected[^1] = 2;
        using MemoryStream content = new(expected);
        using BlobDownloadStreamingResult download = CreateDownloadResult(content, expected.Length);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download);
        SnapshotBlobOperations operations = new(
            container.Object,
            Options.Create(
                new SnapshotBlobStorageOptions
                {
                    MaximumSnapshotDocumentSizeBytes = expected.Length,
                }));
        BinaryData? result = await operations.DownloadAsync("snapshot.json");
        Assert.NotNull(result);
        Assert.Equal(expected, result.ToArray());
        Assert.False(content.CanRead);
    }

    /// <summary>
    ///     Verifies actual bytes are bounded even when the declared content length is absent or incorrect.
    /// </summary>
    /// <param name="declaredLength">The reported document length, or no metadata.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(4L)]
    public async Task DownloadAsyncShouldRejectOversizedBodyRegardlessOfDeclaredLength(
        long? declaredLength
    )
    {
        using MemoryStream content = new([1, 2, 3, 4, 5]);
        using BlobDownloadStreamingResult download = CreateDownloadResult(content, declaredLength);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download);
        SnapshotBlobOperations operations = new(
            container.Object,
            Options.Create(
                new SnapshotBlobStorageOptions
                {
                    MaximumSnapshotDocumentSizeBytes = 4,
                }));
        await Assert.ThrowsAsync<InvalidDataException>(() => operations.DownloadAsync("snapshot.json"));
        Assert.False(content.CanRead);
    }

    /// <summary>
    ///     Verifies declared oversize documents are rejected without reading their bodies and release the response stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldRejectOversizedDeclaredLengthBeforeReading()
    {
        Mock<Stream> content = new(MockBehavior.Strict);
        content.Setup(stream => stream.Close()).CallBase();
        content.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        using BlobDownloadStreamingResult download = CreateDownloadResult(content.Object, 5);
        Mock<BlobContainerClient> container = CreateDownloadContainer(download);
        SnapshotBlobOperations operations = new(
            container.Object,
            Options.Create(
                new SnapshotBlobStorageOptions
                {
                    MaximumSnapshotDocumentSizeBytes = 4,
                }));
        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            operations.DownloadAsync("snapshot.json"));
        Assert.Contains("snapshot.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("4 bytes", exception.Message, StringComparison.Ordinal);
        content.Protected().Verify("Dispose", Times.Once(), ItExpr.Is<bool>(disposing => disposing));
        content.Verify(
            stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies successful downloads return the original document bytes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldReturnDocument()
    {
        const string blobName = "stream/snapshot.json";
        BinaryData document = BinaryData.FromString("{\"schemaVersion\":1}");
        using Stream content = document.ToStream();
        using BlobDownloadStreamingResult download = CreateDownloadResult(content, document.ToMemory().Length);
        using CancellationTokenSource cancellation = new();
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.DownloadStreamingAsync(null, cancellation.Token))
            .ReturnsAsync(Response.FromValue(download, Mock.Of<Response>()));
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        BinaryData? result = await operations.DownloadAsync(blobName, cancellation.Token);
        Assert.NotNull(result);
        Assert.Equal(document.ToArray(), result.ToArray());
        Assert.False(content.CanRead);
        blob.Verify(client => client.DownloadStreamingAsync(null, cancellation.Token), Times.Once);
    }

    /// <summary>
    ///     Verifies an absent blob is the only storage status treated as a missing snapshot.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DownloadAsyncShouldReturnNullForMissingBlob()
    {
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.DownloadStreamingAsync(null, CancellationToken.None))
            .ThrowsAsync(new RequestFailedException(404, "Blob not found."));
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient("missing.json")).Returns(blob.Object);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        Assert.Null(await operations.DownloadAsync("missing.json"));
    }

    /// <summary>
    ///     Verifies every page is enumerated using the supplied stream prefix and cancellation token.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListBlobNamesAsyncShouldEnumerateAllPages()
    {
        using CancellationTokenSource cancellation = new();
        Page<BlobItem>[] pages =
        [
            Page<BlobItem>.FromValues([BlobsModelFactory.BlobItem("stream/1.json")], "next-page", Mock.Of<Response>()),
            Page<BlobItem>.FromValues(
                [BlobsModelFactory.BlobItem("stream/2.json"), BlobsModelFactory.BlobItem("stream/3.json")],
                null,
                Mock.Of<Response>()),
        ];
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobsAsync(BlobTraits.None, BlobStates.None, "stream/", cancellation.Token))
            .Returns(AsyncPageable<BlobItem>.FromPages(pages));
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        List<string> names = [];
        await foreach (string name in operations.ListBlobNamesAsync("stream/", cancellation.Token))
        {
            names.Add(name);
        }

        Assert.Equal(["stream/1.json", "stream/2.json", "stream/3.json"], names);
        container.Verify(
            client => client.GetBlobsAsync(BlobTraits.None, BlobStates.None, "stream/", cancellation.Token),
            Times.Once);
    }

    /// <summary>
    ///     Verifies an empty storage listing does not invent snapshot names.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListBlobNamesAsyncShouldReturnNoNamesForEmptyListing()
    {
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container
            .Setup(client => client.GetBlobsAsync(BlobTraits.None, BlobStates.None, "stream/", CancellationToken.None))
            .Returns(AsyncPageable<BlobItem>.FromPages([Page<BlobItem>.FromValues([], null, Mock.Of<Response>())]));
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        List<string> names = [];
        await foreach (string name in operations.ListBlobNamesAsync("stream/"))
        {
            names.Add(name);
        }

        Assert.Empty(names);
    }

    /// <summary>
    ///     Verifies SDK calls reject missing names or documents before storage access.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task OperationsShouldRejectNullArguments()
    {
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        ArgumentNullException deleteException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.DeleteIfExistsAsync(null!));
        ArgumentNullException downloadException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.DownloadAsync(null!));
        ArgumentNullException uploadNameException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.UploadAsync(null!, BinaryData.FromString("{}")));
        ArgumentNullException uploadDocumentException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.UploadAsync("snapshot.json", null!));
        Assert.Equal("blobName", deleteException.ParamName);
        Assert.Equal("blobName", downloadException.ParamName);
        Assert.Equal("blobName", uploadNameException.ParamName);
        Assert.Equal("document", uploadDocumentException.ParamName);
        container.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies writes preserve the document and JSON content type while forwarding cancellation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UploadAsyncShouldUploadJsonDocument()
    {
        const string blobName = "stream/snapshot.json";
        BinaryData document = BinaryData.FromString("{\"schemaVersion\":1}");
        using CancellationTokenSource cancellation = new();
        Mock<BlobClient> blob = new(MockBehavior.Strict);
        blob.Setup(client => client.UploadAsync(
                document,
                It.Is<BlobUploadOptions>(options => options.HttpHeaders.ContentType == "application/json"),
                cancellation.Token))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        Mock<BlobContainerClient> container = new(MockBehavior.Strict);
        container.Setup(client => client.GetBlobClient(blobName)).Returns(blob.Object);
        SnapshotBlobOperations operations = new(container.Object, Options.Create(new SnapshotBlobStorageOptions()));
        await operations.UploadAsync(blobName, document, cancellation.Token);
        blob.Verify(
            client => client.UploadAsync(
                document,
                It.Is<BlobUploadOptions>(options => options.HttpHeaders.ContentType == "application/json"),
                cancellation.Token),
            Times.Once);
    }
}