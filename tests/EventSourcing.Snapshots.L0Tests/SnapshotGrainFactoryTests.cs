using System;


using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotGrainFactory" />.
/// </summary>
public sealed class SnapshotGrainFactoryTests
{
    private static SnapshotGrainFactory CreateFactory(
        Mock<IGrainFactory>? grainFactoryMock = null,
        Mock<ILogger<SnapshotGrainFactory>>? loggerMock = null
    )
    {
        grainFactoryMock ??= new();
        loggerMock ??= new();
        return new(grainFactoryMock.Object, loggerMock.Object);
    }

    /// <summary>
    ///     Constructor should succeed with valid dependencies.
    /// </summary>
    [Fact]
        public void ConstructorSucceedsWithValidDependencies()
    {
        // Arrange
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<SnapshotGrainFactory>> loggerMock = new();

        // Act
        SnapshotGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(factory);
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when grainFactory is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsArgumentNullExceptionWhenGrainFactoryIsNull()
    {
        // Arrange
        Mock<ILogger<SnapshotGrainFactory>> loggerMock = new();

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new SnapshotGrainFactory(null!, loggerMock.Object));
        Assert.Equal("grainFactory", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainFactory> grainFactoryMock = new();

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new SnapshotGrainFactory(grainFactoryMock.Object, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    /// <summary>
    ///     Verifies that GetSnapshotCacheGrain returns a grain from the underlying factory.
    /// </summary>
    [Fact]
        public void GetSnapshotCacheGrainReturnsGrainFromFactory()
    {
        // Arrange
        SnapshotKey key = new(new("TEST.BROOK", "TestProjection", "entity-1", "hash123"), 5);
        Mock<ISnapshotCacheGrain<SnapshotGrainFactoryTestState>> expectedGrainMock = new();
        Mock<IGrainFactory> grainFactoryMock = new();
        grainFactoryMock
            .Setup(f => f.GetGrain<ISnapshotCacheGrain<SnapshotGrainFactoryTestState>>(key.ToString(), null))
            .Returns(expectedGrainMock.Object);
        SnapshotGrainFactory factory = CreateFactory(grainFactoryMock);

        // Act
        ISnapshotCacheGrain<SnapshotGrainFactoryTestState> result =
            factory.GetSnapshotCacheGrain<SnapshotGrainFactoryTestState>(key);

        // Assert
        Assert.Same(expectedGrainMock.Object, result);
        grainFactoryMock.Verify(
            f => f.GetGrain<ISnapshotCacheGrain<SnapshotGrainFactoryTestState>>(key.ToString(), null),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that GetSnapshotPersisterGrain returns a grain from the underlying factory.
    /// </summary>
    [Fact]
        public void GetSnapshotPersisterGrainReturnsGrainFromFactory()
    {
        // Arrange
        SnapshotKey key = new(new("TEST.BROOK", "TestProjection", "entity-1", "hash123"), 5);
        Mock<ISnapshotPersisterGrain> expectedGrainMock = new();
        Mock<IGrainFactory> grainFactoryMock = new();
        grainFactoryMock.Setup(f => f.GetGrain<ISnapshotPersisterGrain>(key.ToString(), null))
            .Returns(expectedGrainMock.Object);
        SnapshotGrainFactory factory = CreateFactory(grainFactoryMock);

        // Act
        ISnapshotPersisterGrain result = factory.GetSnapshotPersisterGrain(key);

        // Assert
        Assert.Same(expectedGrainMock.Object, result);
        grainFactoryMock.Verify(f => f.GetGrain<ISnapshotPersisterGrain>(key.ToString(), null), Times.Once);
    }
}