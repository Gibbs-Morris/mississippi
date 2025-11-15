using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Tests.Factory;

/// <summary>
///     Tests for <see cref="BrookGrainFactory" />.
/// </summary>
public class BrookGrainFactoryTests
{
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