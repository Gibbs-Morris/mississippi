using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;

using NSubstitute;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

public sealed class SnapshotStorageProviderTests
{
    [Fact]
    public async Task DeleteAsync_WithValidKey_CallsBlobOperations()
    {
        // Arrange
        IBlobSnapshotOperations? blobOperations = Substitute.For<IBlobSnapshotOperations>();
        ILogger<SnapshotStorageProvider>? logger = Substitute.For<ILogger<SnapshotStorageProvider>>();
        SnapshotStorageProvider provider = new(blobOperations, logger);
        SnapshotStreamKey streamKey = new("test-brook", "test", "proj-1", "hash-1");
        SnapshotKey snapshotKey = new(streamKey, 10);

        // Act
        await provider.DeleteAsync(snapshotKey);

        // Assert
        await blobOperations.Received(1).DeleteAsync(snapshotKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Format_ReturnsAzureBlob()
    {
        // Arrange
        IBlobSnapshotOperations? blobOperations = Substitute.For<IBlobSnapshotOperations>();
        ILogger<SnapshotStorageProvider>? logger = Substitute.For<ILogger<SnapshotStorageProvider>>();
        SnapshotStorageProvider provider = new(blobOperations, logger);
        // Act
        string format = provider.Format;
        // Assert
        format.Should().Be("azure-blob");
    }

    [Fact]
    public async Task ReadAsync_WhenBlobDoesNotExist_ReturnsNull()
    {
        // Arrange
        IBlobSnapshotOperations? blobOperations = Substitute.For<IBlobSnapshotOperations>();
        ILogger<SnapshotStorageProvider>? logger = Substitute.For<ILogger<SnapshotStorageProvider>>();
        SnapshotStorageProvider provider = new(blobOperations, logger);
        SnapshotStreamKey streamKey = new("test-brook", "test", "proj-1", "hash-1");
        SnapshotKey snapshotKey = new(streamKey, 10);
        blobOperations.ReadAsync(snapshotKey, Arg.Any<CancellationToken>()).Returns((SnapshotEnvelope?)null);

        // Act
        SnapshotEnvelope? result = await provider.ReadAsync(snapshotKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_WhenBlobExists_ReturnsEnvelope()
    {
        // Arrange
        IBlobSnapshotOperations? blobOperations = Substitute.For<IBlobSnapshotOperations>();
        ILogger<SnapshotStorageProvider>? logger = Substitute.For<ILogger<SnapshotStorageProvider>>();
        SnapshotStorageProvider provider = new(blobOperations, logger);
        SnapshotStreamKey streamKey = new("test-brook", "test", "proj-1", "hash-1");
        SnapshotKey snapshotKey = new(streamKey, 10);
        SnapshotEnvelope expectedEnvelope = new()
        {
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataContentType = "application/json",
            DataSizeBytes = 3,
            ReducerHash = "hash-1",
        };
        blobOperations.ReadAsync(snapshotKey, Arg.Any<CancellationToken>()).Returns(expectedEnvelope);

        // Act
        SnapshotEnvelope? result = await provider.ReadAsync(snapshotKey);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedEnvelope);
    }

    [Fact]
    public async Task WriteAsync_WithValidEnvelope_CallsBlobOperations()
    {
        // Arrange
        IBlobSnapshotOperations? blobOperations = Substitute.For<IBlobSnapshotOperations>();
        ILogger<SnapshotStorageProvider>? logger = Substitute.For<ILogger<SnapshotStorageProvider>>();
        SnapshotStorageProvider provider = new(blobOperations, logger);
        SnapshotStreamKey streamKey = new("test-brook", "test", "proj-1", "hash-1");
        SnapshotKey snapshotKey = new(streamKey, 10);
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataContentType = "application/json",
            DataSizeBytes = 3,
            ReducerHash = "hash-1",
        };

        // Act
        await provider.WriteAsync(snapshotKey, envelope);

        // Assert
        await blobOperations.Received(1)
            .WriteAsync(snapshotKey, envelope, Arg.Any<AccessTier>(), Arg.Any<CancellationToken>());
    }
}