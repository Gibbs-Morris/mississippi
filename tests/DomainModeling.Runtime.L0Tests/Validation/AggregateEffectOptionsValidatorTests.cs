using System;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Validation;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Validation;

/// <summary>
///     Tests for <see cref="AggregateEffectOptionsValidator" />.
/// </summary>
public sealed class AggregateEffectOptionsValidatorTests
{
    private AggregateEffectOptionsValidator Sut { get; } = new();

    /// <summary>
    ///     Iterations of one should return success (boundary).
    /// </summary>
    [Fact]
    public void IterationsOneReturnsSuccess()
    {
        // Arrange
        AggregateEffectOptions options = new()
        {
            MaxEffectIterations = 1,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Negative iterations should return failure.
    /// </summary>
    [Fact]
    public void NegativeIterationsReturnsFail()
    {
        // Arrange
        AggregateEffectOptions options = new()
        {
            MaxEffectIterations = -3,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("-3", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Valid iterations value should return success.
    /// </summary>
    [Fact]
    public void ValidIterationsReturnsSuccess()
    {
        // Arrange
        AggregateEffectOptions options = new()
        {
            MaxEffectIterations = 10,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Zero iterations should return failure.
    /// </summary>
    [Fact]
    public void ZeroIterationsReturnsFail()
    {
        // Arrange
        AggregateEffectOptions options = new()
        {
            MaxEffectIterations = 0,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("MaxEffectIterations", result.FailureMessage, StringComparison.Ordinal);
    }
}