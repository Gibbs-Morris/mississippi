using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.ActionEffects;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="InletSignalRActionEffectOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Action Effects")]
[AllureSubSuite("InletSignalRActionEffectOptions")]
public sealed class InletSignalRActionEffectOptionsTests
{
    /// <summary>
    ///     HubPath should be initializable with custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void HubPathCanBeInitialized()
    {
        // Arrange & Act
        InletSignalRActionEffectOptions sut = new()
        {
            HubPath = "/api/signalr",
        };

        // Assert
        Assert.Equal("/api/signalr", sut.HubPath);
    }

    /// <summary>
    ///     HubPath should be settable to a custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void HubPathCanBeSet()
    {
        // Arrange
        InletSignalRActionEffectOptions sut = new();
        const string customPath = "/custom/signalr/hub";

        // Act
        sut.HubPath = customPath;

        // Assert
        Assert.Equal(customPath, sut.HubPath);
    }

    /// <summary>
    ///     HubPath should default to "/hubs/inlet".
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void HubPathDefaultsToHubsInlet()
    {
        // Arrange & Act
        InletSignalRActionEffectOptions sut = new();

        // Assert
        Assert.Equal("/hubs/inlet", sut.HubPath);
    }
}