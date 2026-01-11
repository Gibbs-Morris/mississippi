using System;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Keys;


namespace Mississippi.Aqueduct.Abstractions.L0Tests.Keys;

/// <summary>
///     Tests for <see cref="SignalRServerDirectoryKey" />.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Server Directory Key")]
public sealed class SignalRServerDirectoryKeyTests
{
    /// <summary>
    ///     Verifies that a valid key can be constructed.
    /// </summary>
    [Fact(DisplayName = "Constructor Creates Valid Key")]
    public void ConstructorShouldCreateValidKey()
    {
        // Act
        SignalRServerDirectoryKey key = new("custom");

        // Assert
        Assert.Equal("custom", key.Value);
    }

    /// <summary>
    ///     Verifies that keys exceeding max length throw ArgumentException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Key Exceeds Max Length")]
    public void ConstructorShouldThrowWhenKeyExceedsMaxLength()
    {
        string longValue = new('a', 5000);
        Assert.Throws<ArgumentException>(() => new SignalRServerDirectoryKey(longValue));
    }

    /// <summary>
    ///     Verifies that null value throws ArgumentNullException.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Value Is Null")]
    public void ConstructorShouldThrowWhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRServerDirectoryKey(null!));
    }

    /// <summary>
    ///     Verifies Default is equal to a key created with "default".
    /// </summary>
    [Fact(DisplayName = "Default Equals Key With Default Value")]
    public void DefaultShouldEqualKeyWithDefaultValue()
    {
        // Arrange
        SignalRServerDirectoryKey defaultKey = SignalRServerDirectoryKey.Default;
        SignalRServerDirectoryKey constructedKey = new("default");

        // Assert
        Assert.Equal(defaultKey, constructedKey);
    }

    /// <summary>
    ///     Verifies that Default returns a key with value "default".
    /// </summary>
    [Fact(DisplayName = "Default Has Value Of Default")]
    public void DefaultShouldHaveValueOfDefault()
    {
        // Act
        SignalRServerDirectoryKey key = SignalRServerDirectoryKey.Default;

        // Assert
        Assert.Equal("default", key.Value);
    }

    /// <summary>
    ///     Verifies record equality works correctly.
    /// </summary>
    [Fact(DisplayName = "Equality Works For Equal Keys")]
    public void EqualityShouldWorkForEqualKeys()
    {
        // Arrange
        SignalRServerDirectoryKey key1 = new("custom");
        SignalRServerDirectoryKey key2 = new("custom");

        // Assert
        Assert.Equal(key1, key2);
    }

    /// <summary>
    ///     Verifies implicit string conversion returns correct value.
    /// </summary>
    [Fact(DisplayName = "Implicit String Conversion Returns Value")]
    public void ImplicitStringConversionShouldReturnValue()
    {
        // Arrange
        SignalRServerDirectoryKey key = new("custom");

        // Act
        string result = key;

        // Assert
        Assert.Equal("custom", result);
    }

    /// <summary>
    ///     Verifies record inequality works correctly.
    /// </summary>
    [Fact(DisplayName = "Inequality Works For Different Keys")]
    public void InequalityShouldWorkForDifferentKeys()
    {
        // Arrange
        SignalRServerDirectoryKey key1 = new("custom1");
        SignalRServerDirectoryKey key2 = new("custom2");

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
        SignalRServerDirectoryKey original = new("custom-directory");

        // Act
        SignalRServerDirectoryKey parsed = SignalRServerDirectoryKey.Parse(original.ToString());

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
        SignalRServerDirectoryKey key = SignalRServerDirectoryKey.Parse("custom");

        // Assert
        Assert.Equal("custom", key.Value);
    }

    /// <summary>
    ///     Verifies that Parse throws when value is null.
    /// </summary>
    [Fact(DisplayName = "Parse Throws When Value Is Null")]
    public void ParseShouldThrowWhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SignalRServerDirectoryKey.Parse(null!));
    }

    /// <summary>
    ///     Verifies ToString returns correct value.
    /// </summary>
    [Fact(DisplayName = "ToString Returns Value")]
    public void ToStringShouldReturnValue()
    {
        // Arrange
        SignalRServerDirectoryKey key = new("custom");

        // Act
        string result = key.ToString();

        // Assert
        Assert.Equal("custom", result);
    }
}