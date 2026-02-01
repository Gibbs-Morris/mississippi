using System.Text.Json;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReservoirDevToolsOptions.
/// </summary>
public sealed class ReservoirDevToolsOptionsTests
{
    /// <summary>
    ///     ActionSanitizer should be settable.
    /// </summary>
    [Fact]
    public void ActionSanitizerIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.ActionSanitizer = action => new
        {
            type = action.GetType().Name,
        };

        // Assert
        Assert.NotNull(options.ActionSanitizer);
    }

    /// <summary>
    ///     AdditionalOptions should be empty by default.
    /// </summary>
    [Fact]
    public void AdditionalOptionsIsEmptyByDefault()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.NotNull(options.AdditionalOptions);
        Assert.Empty(options.AdditionalOptions);
    }

    /// <summary>
    ///     AdditionalOptions should support adding custom options.
    /// </summary>
    [Fact]
    public void AdditionalOptionsSupportCustomOptions()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.AdditionalOptions["features"] = new
        {
            pause = true,
            lock_ = false,
        };
        options.AdditionalOptions["trace"] = true;

        // Assert
        Assert.Equal(2, options.AdditionalOptions.Count);
        Assert.True(options.AdditionalOptions.ContainsKey("features"));
        Assert.True(options.AdditionalOptions.ContainsKey("trace"));
    }

    /// <summary>
    ///     AutoPause should be settable.
    /// </summary>
    [Fact]
    public void AutoPauseIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.AutoPause = true;

        // Assert
        Assert.True(options.AutoPause);
    }

    /// <summary>
    ///     Default Enablement should be Off.
    /// </summary>
    [Fact]
    public void DefaultEnablementIsOff()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.Equal(ReservoirDevToolsEnablement.Off, options.Enablement);
    }

    /// <summary>
    ///     Default IsStrictStateRehydrationEnabled should be false.
    /// </summary>
    [Fact]
    public void DefaultIsStrictStateRehydrationEnabledIsFalse()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.False(options.IsStrictStateRehydrationEnabled);
    }

    /// <summary>
    ///     Enablement can be set to all values.
    /// </summary>
    /// <param name="enablement">The enablement value to test.</param>
    [Theory]
    [InlineData(ReservoirDevToolsEnablement.Off)]
    [InlineData(ReservoirDevToolsEnablement.DevelopmentOnly)]
    [InlineData(ReservoirDevToolsEnablement.Always)]
    public void EnablementCanBeSetToAllValues(
        ReservoirDevToolsEnablement enablement
    )
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.Enablement = enablement;

        // Assert
        Assert.Equal(enablement, options.Enablement);
    }

    /// <summary>
    ///     Latency should be settable.
    /// </summary>
    [Fact]
    public void LatencyIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.Latency = 250;

        // Assert
        Assert.Equal(250, options.Latency);
    }

    /// <summary>
    ///     MaxAge should be settable.
    /// </summary>
    [Fact]
    public void MaxAgeIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.MaxAge = 50;

        // Assert
        Assert.Equal(50, options.MaxAge);
    }

    /// <summary>
    ///     Name should be settable.
    /// </summary>
    [Fact]
    public void NameIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.Name = "TestApp";

        // Assert
        Assert.Equal("TestApp", options.Name);
    }

    /// <summary>
    ///     SerializerOptions can be configured.
    /// </summary>
    [Fact]
    public void SerializerOptionsCanBeConfigured()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.WriteIndented = true;

        // Assert
        Assert.Equal(JsonNamingPolicy.CamelCase, options.SerializerOptions.PropertyNamingPolicy);
        Assert.True(options.SerializerOptions.WriteIndented);
    }

    /// <summary>
    ///     SerializerOptions should have web defaults.
    /// </summary>
    [Fact]
    public void SerializerOptionsHasWebDefaults()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.NotNull(options.SerializerOptions);
        Assert.True(options.SerializerOptions.PropertyNameCaseInsensitive);
    }

    /// <summary>
    ///     StateSanitizer should be settable.
    /// </summary>
    [Fact]
    public void StateSanitizerIsSettable()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act
        options.StateSanitizer = state => new
        {
            filtered = true,
        };

        // Assert
        Assert.NotNull(options.StateSanitizer);
    }
}