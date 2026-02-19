using System;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that <c>default(AggregateKey)</c> invariant breaks are now fixed
///     via C# 14 field keyword null-coalescing in property getter.
/// </summary>
public sealed class AggregateKeyDefaultValueTests
{
    /// <summary>
    ///     FIXED: <c>default(AggregateKey)</c> now returns <see cref="string.Empty" /> for
    ///     <see cref="AggregateKey.EntityId" /> via C# 14 field keyword null-coalescing.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(AggregateKey) previously produced null EntityId. " +
        "C# 14 field keyword null-coalescing now ensures EntityId is string.Empty.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "22-43",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultAggregateKeyHasNonNullEntityId()
    {
        // Arrange – the constructor rejects null
        Assert.Throws<ArgumentNullException>(() => new AggregateKey(null!));

        // Act – default bypasses the constructor
        AggregateKey key = default;

        // Assert – EntityId is now string.Empty, not null
        Assert.Equal(string.Empty, key.EntityId);
    }

    /// <summary>
    ///     FIXED: <see cref="AggregateKey.ToString" /> now returns <see cref="string.Empty" />
    ///     for default-initialized keys instead of null.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: AggregateKey.ToString() previously returned null for default-initialized keys. " +
        "C# 14 field keyword null-coalescing now ensures ToString() returns string.Empty.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "101",
        Severity = "Low",
        Category = "LogicError")]
    public void DefaultAggregateKeyToStringReturnsEmptyString()
    {
        // Arrange
        AggregateKey key = default;

        // Act
        string? result = key.ToString();

        // Assert – ToString returns string.Empty instead of null
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     FIXED: The implicit conversion from <see cref="AggregateKey" /> to string now returns
    ///     <see cref="string.Empty" /> for default-initialized keys instead of null.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: The implicit conversion from AggregateKey to string previously returned null " +
        "for default-initialized keys. C# 14 field keyword null-coalescing now ensures " +
        "the conversion returns string.Empty.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "82-85",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultAggregateKeyImplicitStringConversionReturnsEmptyString()
    {
        // Arrange
        AggregateKey key = default;

        // Act
        string result = key;

        // Assert – implicit conversion now produces string.Empty
        Assert.Equal(string.Empty, result);
    }
}
