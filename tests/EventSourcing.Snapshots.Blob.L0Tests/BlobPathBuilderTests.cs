using FluentAssertions;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

public sealed class BlobPathBuilderTests
{
    [Fact]
    public void BuildPath_WithValidSnapshotKey_ReturnsExpectedPath()
    {
        // Arrange
        SnapshotStreamKey streamKey = new("test-brook", "test-storage", "proj-123", "hash-abc");
        SnapshotKey snapshotKey = new(streamKey, 42);

        // Act
        string result = BlobPathBuilder.BuildPath(snapshotKey);

        // Assert
        result.Should().Be("test-storage/proj-123/hash-abc/42");
    }

    [Fact]
    public void BuildStreamPrefix_WithValidStreamKey_ReturnsExpectedPrefix()
    {
        // Arrange
        SnapshotStreamKey streamKey = new("test-brook", "test-storage", "proj-456", "hash-def");

        // Act
        string result = BlobPathBuilder.BuildStreamPrefix(streamKey);

        // Assert
        result.Should().Be("test-storage/proj-456/hash-def/");
    }
}