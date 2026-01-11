using Allure.Xunit.Attributes;

using Mississippi.Common.Abstractions;


namespace Mississippi.Inlet.Orleans.SignalR.L0Tests;

/// <summary>
///     Tests for <see cref="InletOrleansOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Orleans.SignalR")]
[AllureSuite("Configuration")]
[AllureSubSuite("InletOrleansOptions")]
public sealed class InletOrleansOptionsTests
{
    /// <summary>
    ///     AllClientsStreamNamespace should have correct default value.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void AllClientsStreamNamespaceHasCorrectDefault()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.AllClients, options.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     AllClientsStreamNamespace should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Property Setters")]
    public void AllClientsStreamNamespaceIsSettable()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Act
        options.AllClientsStreamNamespace = "Custom.Namespace";

        // Assert
        Assert.Equal("Custom.Namespace", options.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     HeartbeatIntervalMinutes should have correct default value.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void HeartbeatIntervalMinutesHasCorrectDefault()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Assert
        Assert.Equal(1, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     HeartbeatIntervalMinutes should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Property Setters")]
    public void HeartbeatIntervalMinutesIsSettable()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Act
        options.HeartbeatIntervalMinutes = 5;

        // Assert
        Assert.Equal(5, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     ServerStreamNamespace should have correct default value.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void ServerStreamNamespaceHasCorrectDefault()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.Server, options.ServerStreamNamespace);
    }

    /// <summary>
    ///     ServerStreamNamespace should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Property Setters")]
    public void ServerStreamNamespaceIsSettable()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Act
        options.ServerStreamNamespace = "Custom.Server";

        // Assert
        Assert.Equal("Custom.Server", options.ServerStreamNamespace);
    }

    /// <summary>
    ///     StreamProviderName should have correct default value.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void StreamProviderNameHasCorrectDefault()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamProviderName, options.StreamProviderName);
    }

    /// <summary>
    ///     StreamProviderName should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Property Setters")]
    public void StreamProviderNameIsSettable()
    {
        // Arrange
        InletOrleansOptions options = new();

        // Act
        options.StreamProviderName = "CustomStreams";

        // Assert
        Assert.Equal("CustomStreams", options.StreamProviderName);
    }
}