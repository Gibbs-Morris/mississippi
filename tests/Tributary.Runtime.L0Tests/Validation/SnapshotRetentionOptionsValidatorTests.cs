using System;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Validation;


namespace MississippiTests.Tributary.Runtime.L0Tests.Validation;

/// <summary>
///     Tests for <see cref="SnapshotRetentionOptionsValidator" />.
/// </summary>
public sealed class SnapshotRetentionOptionsValidatorTests
{
    private SnapshotRetentionOptionsValidator Sut { get; } = new();

    /// <summary>
    ///     Modulus of one should return success (boundary).
    /// </summary>
    [Fact]
    public void ModulusOneReturnsSuccess()
    {
        // Arrange
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 1,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Negative modulus should return failure.
    /// </summary>
    [Fact]
    public void NegativeModulusReturnsFail()
    {
        // Arrange
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = -5,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("-5", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Valid modulus should return success.
    /// </summary>
    [Fact]
    public void ValidModulusReturnsSuccess()
    {
        // Arrange
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 100,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Zero modulus should return failure.
    /// </summary>
    [Fact]
    public void ZeroModulusReturnsFail()
    {
        // Arrange
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 0,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("DefaultRetainModulus", result.FailureMessage, StringComparison.Ordinal);
    }
}