using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionGrainFactory" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionGrainFactory")]
#pragma warning disable CS0618 // Type or member is obsolete - testing legacy IBrookDefinition-based methods
public sealed class UxProjectionGrainFactoryTests
{
    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        // Arrange
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionGrainFactory(null!, logger.Object));
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
        Assert.Throws<ArgumentNullException>(() => new UxProjectionGrainFactory(grainFactory.Object, null!));
    }

    /// <summary>
    ///     Resolves a UX projection cursor grain via key and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionCursorGrainWithKeyResolvesAndReturnsInstance()
    {
        // Arrange
        UxProjectionCursorKey key = new("TEST.MODULE.STREAM", "cursor-entity");
        Mock<IUxProjectionCursorGrain> cursorGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IUxProjectionCursorGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(cursorGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionCursorGrain resolved = sut.GetUxProjectionCursorGrain(key);

        // Assert
        Assert.Same(cursorGrain.Object, resolved);
    }

    /// <summary>
    ///     Resolves a UX projection cursor grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionCursorGrainWithTypesResolvesAndReturnsInstance()
    {
        // Arrange
        string entityId = "cursor-entity";
        Mock<IUxProjectionCursorGrain> cursorGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IUxProjectionCursorGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(cursorGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionCursorGrain resolved = sut.GetUxProjectionCursorGrainForGrain<TestProjection, TestGrain>(entityId);

        // Assert
        Assert.Same(cursorGrain.Object, resolved);
    }

    /// <summary>
    ///     Resolves a UX projection grain via Orleans IGrainFactory and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionGrainResolvesAndReturnsInstance()
    {
        // Arrange
        string entityId = "entity123";
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IUxProjectionGrain<TestProjection>>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(projectionGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionGrain<TestProjection> resolved = sut.GetUxProjectionGrain<TestProjection>(entityId);

        // Assert
        Assert.Same(projectionGrain.Object, resolved);
        grainFactory.Verify(
            g => g.GetGrain<IUxProjectionGrain<TestProjection>>(entityId, It.IsAny<string?>()),
            Times.Once);
    }

    /// <summary>
    ///     Factory uses correct entity ID as key.
    /// </summary>
    [Fact]
    public void GetUxProjectionGrainUsesCorrectKeyFormat()
    {
        // Arrange
        string entityId = "entity456";
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        string? capturedKey = null;
        grainFactory.Setup(g => g.GetGrain<IUxProjectionGrain<TestProjection>>(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(projectionGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        sut.GetUxProjectionGrain<TestProjection>(entityId);

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal("entity456", capturedKey);
    }

    /// <summary>
    ///     Resolves a UX projection grain with entity ID and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionGrainWithEntityIdResolvesAndReturnsInstance()
    {
        // Arrange
        string entityId = "projection-entity";
        Mock<IUxProjectionGrain<TestProjection>> projectionGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory.Setup(g => g.GetGrain<IUxProjectionGrain<TestProjection>>(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(projectionGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionGrain<TestProjection> resolved = sut.GetUxProjectionGrain<TestProjection>(entityId);

        // Assert
        Assert.Same(projectionGrain.Object, resolved);
    }

    /// <summary>
    ///     Versioned cache grain uses correct key format.
    /// </summary>
    [Fact]
    public void GetUxProjectionVersionedCacheGrainUsesCorrectKeyFormat()
    {
        // Arrange
        string entityId = "versioned-entity";
        BrookPosition version = new(42);
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        string? capturedKey = null;
        grainFactory
            .Setup(g => g.GetGrain<IUxProjectionVersionedCacheGrain<TestProjection>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(versionedCacheGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        sut.GetUxProjectionVersionedCacheGrainForGrain<TestProjection, TestGrain>(entityId, version);

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal("TEST.MODULE.STREAM|versioned-entity|42", capturedKey);
    }

    /// <summary>
    ///     Resolves a versioned cache grain via key and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionVersionedCacheGrainWithKeyResolvesAndReturnsInstance()
    {
        // Arrange
        UxProjectionVersionedCacheKey versionedCacheKey = new("TEST.MODULE.STREAM", "versioned-entity", new(42));
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory
            .Setup(g => g.GetGrain<IUxProjectionVersionedCacheGrain<TestProjection>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(versionedCacheGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionVersionedCacheGrain<TestProjection> resolved =
            sut.GetUxProjectionVersionedCacheGrain<TestProjection>(versionedCacheKey);

        // Assert
        Assert.Same(versionedCacheGrain.Object, resolved);
    }

    /// <summary>
    ///     Resolves a versioned cache grain via types and returns the instance.
    /// </summary>
    [Fact]
    public void GetUxProjectionVersionedCacheGrainWithTypesResolvesAndReturnsInstance()
    {
        // Arrange
        string entityId = "versioned-entity";
        BrookPosition version = new(42);
        Mock<IUxProjectionVersionedCacheGrain<TestProjection>> versionedCacheGrain = new();
        Mock<IGrainFactory> grainFactory = new(MockBehavior.Strict);
        grainFactory
            .Setup(g => g.GetGrain<IUxProjectionVersionedCacheGrain<TestProjection>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(versionedCacheGrain.Object);
        Mock<ILogger<UxProjectionGrainFactory>> logger = new();
        UxProjectionGrainFactory sut = new(grainFactory.Object, logger.Object);

        // Act
        IUxProjectionVersionedCacheGrain<TestProjection> resolved =
            sut.GetUxProjectionVersionedCacheGrainForGrain<TestProjection, TestGrain>(entityId, version);

        // Assert
        Assert.Same(versionedCacheGrain.Object, resolved);
    }
}