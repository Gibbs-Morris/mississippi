using System;


using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Cursor;
using Mississippi.EventSourcing.Brooks.Abstractions.Reader;
using Mississippi.EventSourcing.Brooks.Abstractions.Writer;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Reader;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Factory;

/// <summary>
///     Tests for <see cref="BrookGrainFactory" />.
/// </summary>
public sealed class BrookGrainFactoryTests
{
    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        // Arrange
        Mock<ILogger<BrookGrainFactory>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BrookGrainFactory(null!, logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainFactory> grainFactory = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BrookGrainFactory(grainFactory.Object, null!));
    }

    /// <summary>
    ///     Resolves an async reader grain with a unique key.
    /// </summary>
    [Fact]
    public void GetBrookAsyncReaderGrainResolvesAndReturnsInstance()
    {
        // Arrange
        BrookKey key = new("type", "id");
        Mock<IBrookAsyncReaderGrain> asyncReader = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IBrookAsyncReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(asyncReader.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IBrookAsyncReaderGrain resolved = sut.GetBrookAsyncReaderGrain(key);

        // Assert
        Assert.Same(asyncReader.Object, resolved);
        grainFactory.Verify(
            g => g.GetGrain<IBrookAsyncReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    /// <summary>
    ///     Async reader grain key should contain unique instance ID.
    /// </summary>
    [Fact]
    public void GetBrookAsyncReaderGrainUsesUniqueInstanceId()
    {
        // Arrange
        BrookKey key = new("type", "id");
        Mock<IBrookAsyncReaderGrain> asyncReader = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        string? capturedKey1 = null;
        string? capturedKey2 = null;
        int callCount = 0;
        grainFactory.Setup(g => g.GetGrain<IBrookAsyncReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((
                grainKey,
                _
            ) =>
            {
                if (callCount == 0)
                {
                    capturedKey1 = grainKey;
                }
                else
                {
                    capturedKey2 = grainKey;
                }

                callCount++;
            })
            .Returns(asyncReader.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act - get two async reader grains for the same brook key
        sut.GetBrookAsyncReaderGrain(key);
        sut.GetBrookAsyncReaderGrain(key);

        // Assert - keys should be different due to unique instance IDs
        Assert.NotNull(capturedKey1);
        Assert.NotNull(capturedKey2);
        Assert.NotEqual(capturedKey1, capturedKey2);
        Assert.Contains("type|id|", capturedKey1, StringComparison.Ordinal);
        Assert.Contains("type|id|", capturedKey2, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Resolves a cursor grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetBrookCursorGrainResolvesAndReturnsInstance()
    {
        // Arrange
        BrookKey key = new("type", "id");
        Mock<IBrookCursorGrain> cursor = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IBrookCursorGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(cursor.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IBrookCursorGrain resolved = sut.GetBrookCursorGrain(key);

        // Assert
        Assert.Same(cursor.Object, resolved);
        string expectedKey = BrookKey.FromBrookKey(key);
        grainFactory.Verify(g => g.GetGrain<IBrookCursorGrain>(expectedKey, It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    ///     Resolves a reader grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetBrookReaderGrainResolvesAndReturnsInstance()
    {
        // Arrange
        BrookKey key = new("type", "id");
        Mock<IBrookReaderGrain> reader = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IBrookReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(reader.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IBrookReaderGrain resolved = sut.GetBrookReaderGrain(key);

        // Assert
        Assert.Same(reader.Object, resolved);
        string expectedKey = BrookKey.FromBrookKey(key);
        grainFactory.Verify(g => g.GetGrain<IBrookReaderGrain>(expectedKey, It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    ///     Resolves a slice reader grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetBrookSliceReaderGrainResolvesAndReturnsInstance()
    {
        // Arrange
        BrookRangeKey rangeKey = BrookRangeKey.FromBrookCompositeKey(new("type", "id"), 0, 100);
        Mock<IBrookSliceReaderGrain> sliceReader = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IBrookSliceReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(sliceReader.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IBrookSliceReaderGrain resolved = sut.GetBrookSliceReaderGrain(rangeKey);

        // Assert
        Assert.Same(sliceReader.Object, resolved);
        grainFactory.Verify(
            g => g.GetGrain<IBrookSliceReaderGrain>(It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    /// <summary>
    ///     Resolves a writer grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetBrookWriterGrainResolvesAndReturnsInstance()
    {
        // Arrange
        BrookKey key = new("type", "id");
        Mock<IBrookWriterGrain> writer = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IBrookWriterGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(writer.Object);
        Mock<ILogger<BrookGrainFactory>> logger = new();
        BrookGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IBrookWriterGrain resolved = sut.GetBrookWriterGrain(key);

        // Assert
        Assert.Same(writer.Object, resolved);
        string expectedKey = BrookKey.FromBrookKey(key);
        grainFactory.Verify(g => g.GetGrain<IBrookWriterGrain>(expectedKey, It.IsAny<string?>()), Times.Once);
    }
}