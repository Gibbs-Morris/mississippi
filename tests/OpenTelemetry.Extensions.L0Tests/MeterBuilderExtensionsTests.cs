using System.Linq;

using Allure.Xunit.Attributes;

using NSubstitute;

using OpenTelemetry.Metrics;


namespace Mississippi.OpenTelemetry.Extensions.L0Tests;

/// <summary>
///     Tests for <see cref="MeterBuilderExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi.OpenTelemetry.Extensions")]
[AllureSuite("Core")]
[AllureSubSuite("MeterBuilderExtensions")]
public sealed class MeterBuilderExtensionsTests
{
    /// <summary>
    ///     AddMississippiMeters should return the same builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Extension Methods")]
    public void AddMississippiMetersReturnsSameBuilder()
    {
        // Arrange
        MeterProviderBuilder mockBuilder = Substitute.For<MeterProviderBuilder>();
        mockBuilder.AddMeter(Arg.Any<string[]>()).Returns(mockBuilder);

        // Act
        MeterProviderBuilder result = mockBuilder.AddMississippiMeters();

        // Assert
        Assert.Same(mockBuilder, result);
    }

    /// <summary>
    ///     AddMississippiMeters should add all Mississippi meters.
    /// </summary>
    [Fact]
    [AllureFeature("Extension Methods")]
    public void AddMississippiMetersRegistersAllMeters()
    {
        // Arrange
        MeterProviderBuilder mockBuilder = Substitute.For<MeterProviderBuilder>();
        mockBuilder.AddMeter(Arg.Any<string[]>()).Returns(mockBuilder);

        // Act
        mockBuilder.AddMississippiMeters();

        // Assert - verify AddMeter was called for each Mississippi meter
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.Aqueduct)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.Aggregates)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.Brooks)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.Inlet)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.Snapshots)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.StorageLocking)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.StorageSnapshots)));
        mockBuilder.Received().AddMeter(Arg.Is<string[]>(m => m.Contains(MississippiMeters.UxProjections)));
    }
}
