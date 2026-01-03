using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Tests for <see cref="SnapshotGrainFactory" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots")]
[AllureSubSuite("Snapshot Grain Factory")]
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
    ///     Verifies that GetSnapshotCacheGrain returns a grain from the underlying factory.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
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
    [AllureFeature("Factory")]
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