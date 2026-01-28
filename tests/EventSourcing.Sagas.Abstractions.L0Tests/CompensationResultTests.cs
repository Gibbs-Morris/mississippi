using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="CompensationResult" /> behavior.
/// </summary>
public sealed class CompensationResultTests
{
    /// <summary>
    ///     Failed should allow null error message.
    /// </summary>
    [Fact]
    public void FailedAllowsNullErrorMessage()
    {
        // Act
        CompensationResult result = CompensationResult.Failed("COMP_ERROR");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("COMP_ERROR", result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Failed should return a failed result with error details.
    /// </summary>
    [Fact]
    public void FailedReturnsFailureWithErrorDetails()
    {
        // Act
        CompensationResult result = CompensationResult.Failed("COMP_ERROR", "Compensation failed");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.WasSkipped);
        Assert.Equal("COMP_ERROR", result.ErrorCode);
        Assert.Equal("Compensation failed", result.ErrorMessage);
    }

    /// <summary>
    ///     Failed with empty error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithEmptyErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed(string.Empty));
    }

    /// <summary>
    ///     Failed with null error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithNullErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed(null!));
    }

    /// <summary>
    ///     Failed with whitespace error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithWhitespaceErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed("   "));
    }

    /// <summary>
    ///     Skipped should return cached instance for efficiency.
    /// </summary>
    [Fact]
    public void SkippedReturnsSameInstance()
    {
        // Act
        CompensationResult result1 = CompensationResult.Skipped();
        CompensationResult result2 = CompensationResult.Skipped();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Skipped should return a skipped result.
    /// </summary>
    [Fact]
    public void SkippedReturnsSkippedResult()
    {
        // Act
        CompensationResult result = CompensationResult.Skipped();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.WasSkipped);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Succeeded should return cached instance for efficiency.
    /// </summary>
    [Fact]
    public void SucceededReturnsSameInstance()
    {
        // Act
        CompensationResult result1 = CompensationResult.Succeeded();
        CompensationResult result2 = CompensationResult.Succeeded();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Succeeded should return a successful result.
    /// </summary>
    [Fact]
    public void SucceededReturnsSuccessResult()
    {
        // Act
        CompensationResult result = CompensationResult.Succeeded();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.WasSkipped);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }
}