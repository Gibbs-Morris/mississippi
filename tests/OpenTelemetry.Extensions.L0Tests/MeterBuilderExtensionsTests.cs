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
        // Note: AddMeter uses params string[], so .AddMeter("name") becomes .AddMeter(new[] { "name" })
        mockBuilder.Received().AddMeter(MississippiMeters.Aqueduct);
        mockBuilder.Received().AddMeter(MississippiMeters.Aggregates);
        mockBuilder.Received().AddMeter(MississippiMeters.Brooks);
        mockBuilder.Received().AddMeter(MississippiMeters.Inlet);
        mockBuilder.Received().AddMeter(MississippiMeters.Snapshots);
        mockBuilder.Received().AddMeter(MississippiMeters.StorageLocking);
        mockBuilder.Received().AddMeter(MississippiMeters.StorageSnapshots);
        mockBuilder.Received().AddMeter(MississippiMeters.UxProjections);
    }
}
