using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="CompensationResult" />.
/// </summary>
public sealed class CompensationResultTests
{
    /// <summary>
    ///     CompensationResult implements record equality.
    /// </summary>
    [Fact]
    public void CompensationResultShouldImplementRecordEquality()
    {
        // Arrange
        CompensationResult result1 = CompensationResult.Failed("CODE", "Message");
        CompensationResult result2 = CompensationResult.Failed("CODE", "Message");

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    ///     Failed returns failed compensation result with error code.
    /// </summary>
    [Fact]
    public void FailedShouldReturnFailedResultWithErrorCode()
    {
        // Act
        CompensationResult result = CompensationResult.Failed("REFUND_FAILED", "Payment provider unavailable");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.WasSkipped);
        Assert.Equal("REFUND_FAILED", result.ErrorCode);
        Assert.Equal("Payment provider unavailable", result.ErrorMessage);
    }

    /// <summary>
    ///     Failed with null error message succeeds.
    /// </summary>
    [Fact]
    public void FailedShouldSupportNullErrorMessage()
    {
        // Act
        CompensationResult result = CompensationResult.Failed("ERROR_CODE");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR_CODE", result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Failed with empty error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithEmptyErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed(string.Empty));
    }

    /// <summary>
    ///     Failed with null error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithNullErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed(null!));
    }

    /// <summary>
    ///     Failed with whitespace error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithWhitespaceErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CompensationResult.Failed("   "));
    }

    /// <summary>
    ///     Skipped returns same instance (singleton).
    /// </summary>
    [Fact]
    public void SkippedShouldReturnSameInstance()
    {
        // Act
        CompensationResult result1 = CompensationResult.Skipped();
        CompensationResult result2 = CompensationResult.Skipped();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Skipped returns skipped compensation result.
    /// </summary>
    [Fact]
    public void SkippedShouldReturnSkippedResult()
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
    ///     Succeeded returns same instance (singleton).
    /// </summary>
    [Fact]
    public void SucceededShouldReturnSameInstance()
    {
        // Act
        CompensationResult result1 = CompensationResult.Succeeded();
        CompensationResult result2 = CompensationResult.Succeeded();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Succeeded returns successful compensation result.
    /// </summary>
    [Fact]
    public void SucceededShouldReturnSuccessfulResult()
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