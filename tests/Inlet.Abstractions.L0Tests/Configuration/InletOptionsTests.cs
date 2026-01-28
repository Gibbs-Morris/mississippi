using System;

using Mississippi.Inlet.Client.Abstractions.Configuration;


namespace Mississippi.Inlet.Abstractions.L0Tests.Configuration;

/// <summary>
///     Tests for <see cref="InletOptions" />.
/// </summary>
public sealed class InletOptionsTests
{
    /// <summary>
    ///     Default AutoReconnect should be true.
    /// </summary>
    [Fact]
    public void AutoReconnectDefaultsToTrue()
    {
        // Act
        InletOptions sut = new();

        // Assert
        Assert.True(sut.AutoReconnect);
    }

    /// <summary>
    ///     AutoReconnect should be settable.
    /// </summary>
    [Fact]
    public void AutoReconnectIsSettable()
    {
        // Arrange
        InletOptions sut = new();

        // Act
        sut.AutoReconnect = false;

        // Assert
        Assert.False(sut.AutoReconnect);
    }

    /// <summary>
    ///     Default DefaultTimeout should be 30 seconds.
    /// </summary>
    [Fact]
    public void DefaultTimeoutDefaultsTo30Seconds()
    {
        // Act
        InletOptions sut = new();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), sut.DefaultTimeout);
    }

    /// <summary>
    ///     DefaultTimeout should be settable.
    /// </summary>
    [Fact]
    public void DefaultTimeoutIsSettable()
    {
        // Arrange
        InletOptions sut = new();

        // Act
        sut.DefaultTimeout = TimeSpan.FromMinutes(1);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), sut.DefaultTimeout);
    }

    /// <summary>
    ///     Default MaxReconnectAttempts should be 5.
    /// </summary>
    [Fact]
    public void MaxReconnectAttemptsDefaultsTo5()
    {
        // Act
        InletOptions sut = new();

        // Assert
        Assert.Equal(5, sut.MaxReconnectAttempts);
    }

    /// <summary>
    ///     MaxReconnectAttempts should be settable.
    /// </summary>
    [Fact]
    public void MaxReconnectAttemptsIsSettable()
    {
        // Arrange
        InletOptions sut = new();

        // Act
        sut.MaxReconnectAttempts = 10;

        // Assert
        Assert.Equal(10, sut.MaxReconnectAttempts);
    }

    /// <summary>
    ///     Default ReconnectDelay should be 5 seconds.
    /// </summary>
    [Fact]
    public void ReconnectDelayDefaultsTo5Seconds()
    {
        // Act
        InletOptions sut = new();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), sut.ReconnectDelay);
    }

    /// <summary>
    ///     ReconnectDelay should be settable.
    /// </summary>
    [Fact]
    public void ReconnectDelayIsSettable()
    {
        // Arrange
        InletOptions sut = new();

        // Act
        sut.ReconnectDelay = TimeSpan.FromSeconds(10);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), sut.ReconnectDelay);
    }
}