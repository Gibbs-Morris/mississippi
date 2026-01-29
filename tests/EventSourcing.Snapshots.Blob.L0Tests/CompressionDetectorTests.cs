using System;

using FluentAssertions;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

public sealed class CompressionDetectorTests
{
    [Fact]
    public void IsBrotli_WithEmptyData_ReturnsFalse()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();
        // Act
        bool result = CompressionDetector.IsBrotli(data);
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCompressed_WithGzipData_ReturnsTrue()
    {
        // Arrange
        byte[] data = { 0x1F, 0x8B, 0x08, 0x00 };
        // Act
        bool result = CompressionDetector.IsCompressed(data);
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCompressed_WithUncompressedData_ReturnsFalse()
    {
        // Arrange
        byte[] data = { 0x00, 0x01, 0x02, 0x03 };
        // Act
        bool result = CompressionDetector.IsCompressed(data);
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsGzip_WithGzipMagicBytes_ReturnsTrue()
    {
        // Arrange
        byte[] data = { 0x1F, 0x8B, 0x08, 0x00 }; // Gzip magic bytes
        // Act
        bool result = CompressionDetector.IsGzip(data);
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsGzip_WithNonGzipData_ReturnsFalse()
    {
        // Arrange
        byte[] data = { 0x00, 0x01, 0x02, 0x03 };
        // Act
        bool result = CompressionDetector.IsGzip(data);
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsGzip_WithTooShortData_ReturnsFalse()
    {
        // Arrange
        byte[] data = { 0x1F };
        // Act
        bool result = CompressionDetector.IsGzip(data);
        // Assert
        result.Should().BeFalse();
    }
}