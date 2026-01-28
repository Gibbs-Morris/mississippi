using Allure.Xunit.Attributes;

using Mississippi.Common.Abstractions;


namespace Mississippi.Inlet.Server.L0Tests;

/// <summary>
///     Tests for <see cref="InletServerOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Server")]
[AllureSuite("Configuration")]
[AllureSubSuite("InletServerOptions")]
public sealed class InletServerOptionsTests
{
    /// <summary>
    ///     AllClientsStreamNamespace should have correct default value.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void AllClientsStreamNamespaceHasCorrectDefault()
    {
        // Arrange
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

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
        InletServerOptions options = new();

        // Act
        options.StreamProviderName = "CustomStreams";

        // Assert
        Assert.Equal("CustomStreams", options.StreamProviderName);
    }
}