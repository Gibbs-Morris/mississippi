using System;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that <c>default(AggregateKey)</c> bypasses constructor validation,
///     producing a struct with a null <see cref="AggregateKey.EntityId" />.
/// </summary>
public sealed class AggregateKeyDefaultValueTests
{
    /// <summary>
    ///     The <see cref="AggregateKey" /> constructor validates that the entity ID is not null,
    ///     but <c>default(AggregateKey)</c> bypasses the constructor entirely, leaving
    ///     <see cref="AggregateKey.EntityId" /> as null. Any code receiving an
    ///     <see cref="AggregateKey" /> may assume the entity ID is always non-null because
    ///     the constructor enforces it, but default-initialized structs violate this invariant.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(AggregateKey) bypasses the constructor's null validation, " +
        "producing a struct where EntityId is null. Code that accepts AggregateKey " +
        "may assume EntityId is always non-null because the constructor enforces it, " +
        "but default-initialized structs violate this invariant.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "22-43",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultAggregateKeyHasNullEntityId()
    {
        // Arrange – the constructor rejects null
        Assert.Throws<ArgumentNullException>(() => new AggregateKey(null!));

        // Act – default bypasses the constructor
        AggregateKey key = default;

        // Assert – EntityId is null despite the constructor preventing it
        Assert.Null(key.EntityId);
    }

    /// <summary>
    ///     <see cref="AggregateKey.ToString" /> returns <see cref="AggregateKey.EntityId" />
    ///     directly. For a default-initialized key, this returns null, which violates the
    ///     general <see cref="object.ToString" /> contract that expects a non-null result.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "AggregateKey.ToString() returns EntityId directly. For a default-initialized key " +
        "ToString() returns null, violating the general contract of object.ToString() which " +
        "is expected to return a non-null string representation.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "101",
        Severity = "Low",
        Category = "LogicError")]
    public void DefaultAggregateKeyToStringReturnsNull()
    {
        // Arrange
        AggregateKey key = default;

        // Act
        string? result = key.ToString();

        // Assert – ToString returns null instead of a valid string
        Assert.Null(result);
    }

    /// <summary>
    ///     The implicit conversion <c>operator string(AggregateKey key)</c> returns
    ///     <see cref="AggregateKey.EntityId" /> directly. For a default key, this produces
    ///     a null string from a conversion operator typed as returning non-nullable string.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "The implicit conversion from AggregateKey to string returns EntityId directly. " +
        "For a default-initialized key this yields null from an operator typed as returning " +
        "non-nullable string, potentially causing NullReferenceExceptions downstream.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs",
        LineNumbers = "82-85",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultAggregateKeyImplicitStringConversionReturnsNull()
    {
        // Arrange
        AggregateKey key = default;

        // Act
        string result = key;

        // Assert – implicit conversion produces null from a non-nullable return type
        Assert.Null(result);
    }
}
