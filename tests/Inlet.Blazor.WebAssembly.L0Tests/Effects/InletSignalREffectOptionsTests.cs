using Allure.Xunit.Attributes;

using Mississippi.Inlet.Blazor.WebAssembly.Effects;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="InletSignalREffectOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Effects")]
[AllureSubSuite("InletSignalREffectOptions")]
public sealed class InletSignalREffectOptionsTests
{
    /// <summary>
    ///     HubPath should default to "/hubs/inlet".
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void HubPathDefaultsToHubsInlet()
    {
        // Arrange & Act
        InletSignalREffectOptions sut = new();

        // Assert
        Assert.Equal("/hubs/inlet", sut.HubPath);
    }

    /// <summary>
    ///     HubPath should be settable to a custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void HubPathCanBeSet()
    {
        // Arrange
        InletSignalREffectOptions sut = new();
        const string customPath = "/custom/signalr/hub";

        // Act
        sut.HubPath = customPath;

        // Assert
        Assert.Equal(customPath, sut.HubPath);
    }

    /// <summary>
    ///     HubPath should be initializable with custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void HubPathCanBeInitialized()
    {
        // Arrange & Act
        InletSignalREffectOptions sut = new()
        {
            HubPath = "/api/signalr",
        };

        // Assert
        Assert.Equal("/api/signalr", sut.HubPath);
    }
}
