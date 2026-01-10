using System;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Keys;


namespace Mississippi.Aqueduct.Abstractions.L0Tests.Keys;

/// <summary>
///     Tests for <see cref="SignalRClientKey" />.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Client Key")]
public sealed class SignalRClientKeyTests
{
    /// <summary>
    ///     Verifies that a valid key can be constructed.
    /// </summary>
    [Fact(DisplayName = "Constructor Creates Valid Key")]
    public void ConstructorShouldCreateValidKey()
    {
        // Act
        SignalRClientKey key = new("TestHub", "conn123");

        // Assert
        Assert.Equal("TestHub", key.HubName);
        Assert.Equal("conn123", key.ConnectionId);
    }

    /// <summary>
    ///     Verifies that connection ID containing separator throws ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ConnectionId Contains Separator")]
    public void ConstructorShouldThrowWhenConnectionIdContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new SignalRClientKey("TestHub", "conn|123"));
    }

    /// <summary>
    ///     Verifies that null connection ID throws ArgumentNullException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ConnectionId Is Null")]
    public void ConstructorShouldThrowWhenConnectionIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey("TestHub", null!));
    }

    /// <summary>
    ///     Verifies that hub name containing separator throws ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When HubName Contains Separator")]
    public void ConstructorShouldThrowWhenHubNameContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new SignalRClientKey("Test|Hub", "conn123"));
    }

    /// <summary>
    ///     Verifies that null hub name throws ArgumentNullException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When HubName Is Null")]
    public void ConstructorShouldThrowWhenHubNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey(null!, "conn123"));
    }

    /// <summary>
    ///     Verifies that keys exceeding max length throw ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Key Exceeds Max Length")]
    public void ConstructorShouldThrowWhenKeyExceedsMaxLength()
    {
        string longHub = new('a', 3000);
        string longConn = new('b', 2000);
        Assert.Throws<ArgumentException>(() => new SignalRClientKey(longHub, longConn));
    }

    /// <summary>
    ///     Verifies record equality works correctly.
    /// </summary>
    [Fact(DisplayName = "Equality Works For Equal Keys")]
    public void EqualityShouldWorkForEqualKeys()
    {
        // Arrange
        SignalRClientKey key1 = new("TestHub", "conn123");
        SignalRClientKey key2 = new("TestHub", "conn123");

        // Assert
        Assert.Equal(key1, key2);
    }

    /// <summary>
    ///     Verifies implicit string conversion returns correct format.
    /// </summary>
    [Fact(DisplayName = "Implicit String Conversion Returns Correct Format")]
    public void ImplicitStringConversionShouldReturnCorrectFormat()
    {
        // Arrange
        SignalRClientKey key = new("TestHub", "conn123");

        // Act
        string result = key;

        // Assert
        Assert.Equal("TestHub|conn123", result);
    }

    /// <summary>
    ///     Verifies record inequality works correctly.
    /// </summary>
    [Fact(DisplayName = "Inequality Works For Different Keys")]
    public void InequalityShouldWorkForDifferentKeys()
    {
        // Arrange
        SignalRClientKey key1 = new("TestHub", "conn123");
        SignalRClientKey key2 = new("TestHub", "conn456");

        // Assert
        Assert.NotEqual(key1, key2);
    }

    /// <summary>
    ///     Verifies Parse and ToString round-trip correctly.
    /// </summary>
    [Fact(DisplayName = "Parse And ToString Round Trip")]
    public void ParseAndToStringShouldRoundTrip()
    {
        // Arrange
        SignalRClientKey original = new("MyHub", "connection-abc");

        // Act
        SignalRClientKey parsed = SignalRClientKey.Parse(original.ToString());

        // Assert
        Assert.Equal(original, parsed);
    }

    /// <summary>
    ///     Verifies that Parse correctly parses a valid key string.
    /// </summary>
    [Fact(DisplayName = "Parse Returns Valid Key")]
    public void ParseShouldReturnValidKey()
    {
        // Act
        SignalRClientKey key = SignalRClientKey.Parse("TestHub|conn123");

        // Assert
        Assert.Equal("TestHub", key.HubName);
        Assert.Equal("conn123", key.ConnectionId);
    }

    /// <summary>
    ///     Verifies that Parse throws when separator is missing.
    /// </summary>
    [Fact(DisplayName = "Parse Throws When Separator Is Missing")]
    public void ParseShouldThrowWhenSeparatorIsMissing()
    {
        Assert.Throws<FormatException>(() => SignalRClientKey.Parse("TestHubconn123"));
    }

    /// <summary>
    ///     Verifies that Parse throws when value is null.
    /// </summary>
    [Fact(DisplayName = "Parse Throws When Value Is Null")]
    public void ParseShouldThrowWhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalRClientKey.Parse(null!));
    }

    /// <summary>
    ///     Verifies ToString returns correct format.
    /// </summary>
    [Fact(DisplayName = "ToString Returns Correct Format")]
    public void ToStringShouldReturnCorrectFormat()
    {
        // Arrange
        SignalRClientKey key = new("TestHub", "conn123");

        // Act
        string result = key.ToString();

        // Assert
        Assert.Equal("TestHub|conn123", result);
    }
}