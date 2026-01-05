using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Client.L0Tests;

/// <summary>
///     Tests for <see cref="RipplesClientOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("Client")]
[AllureSubSuite("RipplesClientOptions")]
public sealed class RipplesClientOptionsTests
{
    /// <summary>
    ///     BaseApiUri can be set to a value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void BaseApiUriCanBeSet()
    {
        // Arrange
        RipplesClientOptions sut = new();
        Uri expected = new("https://api.example.com");

        // Act
        sut.BaseApiUri = expected;

        // Assert
        sut.BaseApiUri.Should().Be(expected);
    }

    /// <summary>
    ///     Default values are correctly initialized.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void DefaultValuesAreCorrectlyInitialized()
    {
        // Arrange & Act
        RipplesClientOptions sut = new();

        // Assert
        sut.BaseApiUri.Should().BeNull();
        sut.SignalRHubPath.Should().Be("/hubs/projections");
        sut.EnableAutoReconnect.Should().BeTrue();
        sut.MaxReconnectAttempts.Should().Be(10);
        sut.InitialReconnectDelay.Should().Be(TimeSpan.FromSeconds(1));
        sut.MaxReconnectDelay.Should().Be(TimeSpan.FromSeconds(30));
        sut.HttpTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    ///     EnableAutoReconnect can be disabled.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void EnableAutoReconnectCanBeDisabled()
    {
        // Arrange
        RipplesClientOptions sut = new();

        // Act
        sut.EnableAutoReconnect = false;

        // Assert
        sut.EnableAutoReconnect.Should().BeFalse();
    }

    /// <summary>
    ///     HttpTimeout can be customized.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void HttpTimeoutCanBeCustomized()
    {
        // Arrange
        RipplesClientOptions sut = new();
        TimeSpan expected = TimeSpan.FromMinutes(2);

        // Act
        sut.HttpTimeout = expected;

        // Assert
        sut.HttpTimeout.Should().Be(expected);
    }

    /// <summary>
    ///     InitialReconnectDelay can be customized.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void InitialReconnectDelayCanBeCustomized()
    {
        // Arrange
        RipplesClientOptions sut = new();
        TimeSpan expected = TimeSpan.FromMilliseconds(500);

        // Act
        sut.InitialReconnectDelay = expected;

        // Assert
        sut.InitialReconnectDelay.Should().Be(expected);
    }

    /// <summary>
    ///     MaxReconnectAttempts can be set to zero for unlimited.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void MaxReconnectAttemptsCanBeSetToZero()
    {
        // Arrange
        RipplesClientOptions sut = new();

        // Act
        sut.MaxReconnectAttempts = 0;

        // Assert
        sut.MaxReconnectAttempts.Should().Be(0);
    }

    /// <summary>
    ///     MaxReconnectDelay can be customized.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void MaxReconnectDelayCanBeCustomized()
    {
        // Arrange
        RipplesClientOptions sut = new();
        TimeSpan expected = TimeSpan.FromMinutes(1);

        // Act
        sut.MaxReconnectDelay = expected;

        // Assert
        sut.MaxReconnectDelay.Should().Be(expected);
    }

    /// <summary>
    ///     SignalRHubPath can be set to a custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void SignalRHubPathCanBeSet()
    {
        // Arrange
        RipplesClientOptions sut = new();

        // Act
        sut.SignalRHubPath = "/custom/hub";

        // Assert
        sut.SignalRHubPath.Should().Be("/custom/hub");
    }
}