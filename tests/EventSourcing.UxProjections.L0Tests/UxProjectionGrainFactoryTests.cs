using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

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
public sealed class UxProjectionGrainFactoryTests
{
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
        IUxProjectionGrain<TestProjection> resolved =
            sut.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(entityId);

        // Assert
        Assert.Same(projectionGrain.Object, resolved);
        UxProjectionKey expectedKey = UxProjectionKey.For<TestProjection, TestBrookDefinition>(entityId);
        string expectedKeyString = expectedKey.ToString();
        grainFactory.Verify(
            g => g.GetGrain<IUxProjectionGrain<TestProjection>>(expectedKeyString, It.IsAny<string?>()),
            Times.Once);
    }

    /// <summary>
    ///     Factory uses correct projection key format for different projection types.
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
        sut.GetUxProjectionGrain<TestProjection, TestBrookDefinition>(entityId);

        // Assert
        Assert.NotNull(capturedKey);
        Assert.Equal("TestProjection|TEST.MODULE.STREAM|entity456", capturedKey);
    }
}