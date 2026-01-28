using System;



namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="OperationResult{T}" /> behavior.
/// </summary>
public class OperationResultOfTTests
{
    /// <summary>
    ///     Fail should create a failed result with the specified error details.
    /// </summary>
    [Fact]
    public void FailCreatesFailedResult()
    {
        OperationResult<string> result = OperationResult.Fail<string>("ERROR_CODE", "Error message");
        Assert.False(result.Success);
        Assert.Equal("ERROR_CODE", result.ErrorCode);
        Assert.Equal("Error message", result.ErrorMessage);
        Assert.Null(result.Value);
    }

    /// <summary>
    ///     Fail should throw ArgumentNullException when error code is null.
    /// </summary>
    [Fact]
    public void FailThrowsArgumentNullExceptionWhenErrorCodeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => OperationResult.Fail<int>(null!, "message"));
    }

    /// <summary>
    ///     Fail should throw ArgumentNullException when error message is null.
    /// </summary>
    [Fact]
    public void FailThrowsArgumentNullExceptionWhenErrorMessageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => OperationResult.Fail<int>("CODE", null!));
    }

    /// <summary>
    ///     Fail should throw when error code is empty or whitespace.
    /// </summary>
    /// <param name="errorCode">The error code to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailThrowsWhenErrorCodeIsEmptyOrWhitespace(
        string errorCode
    )
    {
        Assert.Throws<ArgumentException>(() => OperationResult.Fail<int>(errorCode, "message"));
    }

    /// <summary>
    ///     Fail should throw when error message is empty or whitespace.
    /// </summary>
    /// <param name="errorMessage">The error message to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailThrowsWhenErrorMessageIsEmptyOrWhitespace(
        string errorMessage
    )
    {
        Assert.Throws<ArgumentException>(() => OperationResult.Fail<int>("CODE", errorMessage));
    }

    /// <summary>
    ///     Ok should create a successful result with the specified value.
    /// </summary>
    [Fact]
    public void OkCreatesSuccessResultWithValue()
    {
        OperationResult<int> result = OperationResult.Ok(42);
        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Ok with reference type should create a successful result.
    /// </summary>
    [Fact]
    public void OkWithReferenceTypeCreatesSuccessResult()
    {
        var value = new
        {
            Name = "Test",
        };
        OperationResult<object> result = OperationResult.Ok<object>(value);
        Assert.True(result.Success);
        Assert.Same(value, result.Value);
    }

    /// <summary>
    ///     ToResult on failed result should return failed OperationResult.
    /// </summary>
    [Fact]
    public void ToResultOnFailedReturnsFailedOperationResult()
    {
        OperationResult<int> typedResult = OperationResult.Fail<int>("CODE", "message");
        OperationResult result = typedResult.ToResult();
        Assert.False(result.Success);
        Assert.Equal("CODE", result.ErrorCode);
        Assert.Equal("message", result.ErrorMessage);
    }

    /// <summary>
    ///     ToResult on success should return successful OperationResult.
    /// </summary>
    [Fact]
    public void ToResultOnSuccessReturnsSuccessOperationResult()
    {
        OperationResult<int> typedResult = OperationResult.Ok(42);
        OperationResult result = typedResult.ToResult();
        Assert.True(result.Success);
    }
}