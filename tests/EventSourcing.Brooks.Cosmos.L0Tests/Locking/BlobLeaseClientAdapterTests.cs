using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Mississippi.EventSourcing.Cosmos.Locking;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Locking;

/// <summary>
///     Tests for <see cref="BlobLeaseClientAdapter" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Cosmos")]
[AllureSubSuite("Blob Lease Client Adapter")]
public sealed class BlobLeaseClientAdapterTests
{
    /// <summary>
    ///     AcquireAsync should delegate to the inner client.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AcquireAsyncShouldDelegateToInner()
    {
        // Arrange
        TimeSpan duration = TimeSpan.FromSeconds(30);
        RequestConditions conditions = new()
        {
            IfMatch = new ETag("test-etag"),
        };
        using CancellationTokenSource cts = new();
        Mock<Response<BlobLease>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.AcquireAsync(duration, conditions, cts.Token)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<BlobLease> result = await adapter.AcquireAsync(duration, conditions, cts.Token);

        // Assert
        Assert.Same(responseMock.Object, result);
        innerMock.Verify(c => c.AcquireAsync(duration, conditions, cts.Token), Times.Once);
    }

    /// <summary>
    ///     AcquireAsync should work with null conditions (default parameter).
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AcquireAsyncShouldWorkWithNullConditions()
    {
        // Arrange
        TimeSpan duration = TimeSpan.FromSeconds(15);
        Mock<Response<BlobLease>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.AcquireAsync(duration, null, CancellationToken.None)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<BlobLease> result = await adapter.AcquireAsync(duration);

        // Assert
        Assert.Same(responseMock.Object, result);
    }

    /// <summary>
    ///     LeaseId should return the inner client's LeaseId.
    /// </summary>
    [Fact]
    public void LeaseIdShouldReturnInnerLeaseId()
    {
        // Arrange
        const string expectedLeaseId = "test-lease-id";
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.LeaseId).Returns(expectedLeaseId);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        string result = adapter.LeaseId;

        // Assert
        Assert.Equal(expectedLeaseId, result);
    }

    /// <summary>
    ///     ReleaseAsync should delegate to the inner client.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ReleaseAsyncShouldDelegateToInner()
    {
        // Arrange
        RequestConditions conditions = new()
        {
            IfMatch = new ETag("test-etag"),
        };
        using CancellationTokenSource cts = new();
        Mock<Response<ReleasedObjectInfo>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.ReleaseAsync(conditions, cts.Token)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<ReleasedObjectInfo> result = await adapter.ReleaseAsync(conditions, cts.Token);

        // Assert
        Assert.Same(responseMock.Object, result);
        innerMock.Verify(c => c.ReleaseAsync(conditions, cts.Token), Times.Once);
    }

    /// <summary>
    ///     ReleaseAsync should work with null conditions (default parameter).
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ReleaseAsyncShouldWorkWithNullConditions()
    {
        // Arrange
        Mock<Response<ReleasedObjectInfo>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.ReleaseAsync(null, default)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<ReleasedObjectInfo> result = await adapter.ReleaseAsync();

        // Assert
        Assert.Same(responseMock.Object, result);
    }

    /// <summary>
    ///     RenewAsync should delegate to the inner client.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncShouldDelegateToInner()
    {
        // Arrange
        RequestConditions conditions = new()
        {
            IfMatch = new ETag("test-etag"),
        };
        using CancellationTokenSource cts = new();
        Mock<Response<BlobLease>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.RenewAsync(conditions, cts.Token)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<BlobLease> result = await adapter.RenewAsync(conditions, cts.Token);

        // Assert
        Assert.Same(responseMock.Object, result);
        innerMock.Verify(c => c.RenewAsync(conditions, cts.Token), Times.Once);
    }

    /// <summary>
    ///     RenewAsync should work with null conditions (default parameter).
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RenewAsyncShouldWorkWithNullConditions()
    {
        // Arrange
        Mock<Response<BlobLease>> responseMock = new();
        Mock<BlobLeaseClient> innerMock = new();
        innerMock.Setup(c => c.RenewAsync(null, default)).ReturnsAsync(responseMock.Object);
        BlobLeaseClientAdapter adapter = new(innerMock.Object);

        // Act
        Response<BlobLease> result = await adapter.RenewAsync();

        // Assert
        Assert.Same(responseMock.Object, result);
    }
}