using FluentAssertions;

using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

public sealed class SnapshotStorageOptionsTests
{
    [Fact]
    public void BlobServiceClientKey_CanBeSet()
    {
        // Arrange
        SnapshotStorageOptions options = new();
        // Act
        options.BlobServiceClientKey = "custom-key";
        // Assert
        options.BlobServiceClientKey.Should().Be("custom-key");
    }

    [Fact]
    public void ContainerId_CanBeSet()
    {
        // Arrange
        SnapshotStorageOptions options = new();
        // Act
        options.ContainerId = "custom-container";
        // Assert
        options.ContainerId.Should().Be("custom-container");
    }

    [Fact]
    public void DefaultBlobServiceClientKey_IsBlobSnapshotsClient()
    {
        // Arrange
        SnapshotStorageOptions options = new();
        // Act & Assert
        options.BlobServiceClientKey.Should().Be(MississippiDefaults.ServiceKeys.BlobSnapshotsClient);
    }

    [Fact]
    public void DefaultContainerId_IsSnapshots()
    {
        // Arrange
        SnapshotStorageOptions options = new();
        // Act & Assert
        options.ContainerId.Should().Be(MississippiDefaults.ContainerIds.Snapshots);
    }
}