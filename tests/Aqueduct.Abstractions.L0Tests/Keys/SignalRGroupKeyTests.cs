using System;

using Mississippi.Aqueduct.Abstractions.Keys;


namespace Mississippi.Aqueduct.Abstractions.L0Tests.Keys;

/// <summary>
///     Tests for <see cref="SignalRGroupKey" />.
/// </summary>
public sealed class SignalRGroupKeyTests
{
    /// <summary>
    ///     Verifies that a valid key can be constructed.
    /// </summary>
    [Fact(DisplayName = "Constructor Creates Valid Key")]
    public void ConstructorShouldCreateValidKey()
    {
        // Act
        SignalRGroupKey key = new("TestHub", "group1");

        // Assert
        Assert.Equal("TestHub", key.HubName);
        Assert.Equal("group1", key.GroupName);
    }

    /// <summary>
    ///     Verifies that group name containing separator throws ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When GroupName Contains Separator")]
    public void ConstructorShouldThrowWhenGroupNameContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new SignalRGroupKey("TestHub", "group:1"));
    }

    /// <summary>
    ///     Verifies that null group name throws ArgumentNullException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When GroupName Is Null")]
    public void ConstructorShouldThrowWhenGroupNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey("TestHub", null!));
    }

    /// <summary>
    ///     Verifies that hub name containing separator throws ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When HubName Contains Separator")]
    public void ConstructorShouldThrowWhenHubNameContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new SignalRGroupKey("Test:Hub", "group1"));
    }

    /// <summary>
    ///     Verifies that null hub name throws ArgumentNullException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When HubName Is Null")]
    public void ConstructorShouldThrowWhenHubNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey(null!, "group1"));
    }

    /// <summary>
    ///     Verifies that keys exceeding max length throw ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Key Exceeds Max Length")]
    public void ConstructorShouldThrowWhenKeyExceedsMaxLength()
    {
        string longHub = new('a', 3000);
        string longGroup = new('b', 2000);
        Assert.Throws<ArgumentException>(() => new SignalRGroupKey(longHub, longGroup));
    }

    /// <summary>
    ///     Verifies record equality works correctly.
    /// </summary>
    [Fact(DisplayName = "Equality Works For Equal Keys")]
    public void EqualityShouldWorkForEqualKeys()
    {
        // Arrange
        SignalRGroupKey key1 = new("TestHub", "group1");
        SignalRGroupKey key2 = new("TestHub", "group1");

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
        SignalRGroupKey key = new("TestHub", "group1");

        // Act
        string result = key;

        // Assert
        Assert.Equal("TestHub:group1", result);
    }

    /// <summary>
    ///     Verifies record inequality works correctly.
    /// </summary>
    [Fact(DisplayName = "Inequality Works For Different Keys")]
    public void InequalityShouldWorkForDifferentKeys()
    {
        // Arrange
        SignalRGroupKey key1 = new("TestHub", "group1");
        SignalRGroupKey key2 = new("TestHub", "group2");

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
        SignalRGroupKey original = new("MyHub", "admins");

        // Act
        SignalRGroupKey parsed = SignalRGroupKey.Parse(original.ToString());

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
        SignalRGroupKey key = SignalRGroupKey.Parse("TestHub:group1");

        // Assert
        Assert.Equal("TestHub", key.HubName);
        Assert.Equal("group1", key.GroupName);
    }

    /// <summary>
    ///     Verifies that Parse throws when separator is missing.
    /// </summary>
    [Fact(DisplayName = "Parse Throws When Separator Is Missing")]
    public void ParseShouldThrowWhenSeparatorIsMissing()
    {
        Assert.Throws<FormatException>(() => SignalRGroupKey.Parse("TestHubgroup1"));
    }

    /// <summary>
    ///     Verifies that Parse throws when value is null.
    /// </summary>
    [Fact(DisplayName = "Parse Throws When Value Is Null")]
    public void ParseShouldThrowWhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalRGroupKey.Parse(null!));
    }

    /// <summary>
    ///     Verifies ToString returns correct format.
    /// </summary>
    [Fact(DisplayName = "ToString Returns Correct Format")]
    public void ToStringShouldReturnCorrectFormat()
    {
        // Arrange
        SignalRGroupKey key = new("TestHub", "group1");

        // Act
        string result = key.ToString();

        // Assert
        Assert.Equal("TestHub:group1", result);
    }
}