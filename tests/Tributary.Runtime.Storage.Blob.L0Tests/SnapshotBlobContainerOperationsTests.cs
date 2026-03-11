using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Runtime.Storage.Blob.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobContainerOperations" />.
/// </summary>
public sealed class SnapshotBlobContainerOperationsTests
{
    /// <summary>
    ///     Ensures DeleteBlobIfExistsAsync returns the SDK result.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteBlobIfExistsAsyncShouldReturnSdkValue()
    {
        Mock<BlobContainerClient> containerClient = new();
        containerClient.Setup(c => c.DeleteBlobIfExistsAsync(
                "blob",
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        SnapshotBlobContainerOperations operations = new(containerClient.Object, NullLogger<SnapshotBlobContainerOperations>.Instance);
        bool deleted = await operations.DeleteBlobIfExistsAsync("blob", CancellationToken.None);
        Assert.True(deleted);
    }

    /// <summary>
    ///     Ensures container initialization calls CreateIfNotExistsAsync.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task EnsureContainerExistsAsyncShouldCreateContainer()
    {
        Mock<BlobContainerClient> containerClient = new();
        containerClient.Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        SnapshotBlobContainerOperations operations = new(containerClient.Object, NullLogger<SnapshotBlobContainerOperations>.Instance);
        await operations.EnsureContainerExistsAsync(CancellationToken.None);
        containerClient.Verify(
            c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures missing blobs return null on read.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DownloadBlobAsyncShouldReturnNullWhenMissing()
    {
        Mock<BlobContainerClient> containerClient = new();
        Mock<BlobClient> blobClient = new();
        containerClient.Setup(c => c.GetBlobClient("blob")).Returns(blobClient.Object);
        blobClient.Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "missing"));
        SnapshotBlobContainerOperations operations = new(containerClient.Object, NullLogger<SnapshotBlobContainerOperations>.Instance);
        SnapshotBlobDownloadResult? result = await operations.DownloadBlobAsync("blob", CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Ensures UploadBlobAsync uploads with Blob metadata.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task UploadBlobAsyncShouldUploadContent()
    {
        Mock<BlobContainerClient> containerClient = new();
        Mock<BlobClient> blobClient = new();
        containerClient.Setup(c => c.GetBlobClient("blob")).Returns(blobClient.Object);
        blobClient.Setup(b => b.UploadAsync(
                It.IsAny<BinaryData>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        SnapshotBlobContainerOperations operations = new(containerClient.Object, NullLogger<SnapshotBlobContainerOperations>.Instance);
        await operations.UploadBlobAsync(
            "blob",
            new(new byte[] { 1, 2 }, "application/json", 2, false),
            CancellationToken.None);
        blobClient.Verify(
            b => b.UploadAsync(
                It.IsAny<BinaryData>(),
                It.Is<BlobUploadOptions>(options =>
                    options.HttpHeaders.ContentType == "application/json" &&
                    options.Metadata["data-size-bytes"] == "2" &&
                    options.Metadata["is-compressed"] == bool.FalseString),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
