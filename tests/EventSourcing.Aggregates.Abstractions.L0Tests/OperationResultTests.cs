using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="OperationResult" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Abstractions")]
[AllureSubSuite("Operation Result")]
public class OperationResultTests
{
    /// <summary>
    ///     Fail should create a failed result with the specified error details.
    /// </summary>
    [Fact]
    public void FailCreatesFailedResult()
    {
        OperationResult result = OperationResult.Fail("ERROR_CODE", "Error message");
        Assert.False(result.Success);
        Assert.Equal("ERROR_CODE", result.ErrorCode);
        Assert.Equal("Error message", result.ErrorMessage);
    }

    /// <summary>
    ///     Fail should throw ArgumentNullException when error code is null.
    /// </summary>
    [Fact]
    public void FailThrowsArgumentNullExceptionWhenErrorCodeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => OperationResult.Fail(null!, "message"));
    }

    /// <summary>
    ///     Fail should throw ArgumentNullException when error message is null.
    /// </summary>
    [Fact]
    public void FailThrowsArgumentNullExceptionWhenErrorMessageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => OperationResult.Fail("CODE", null!));
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
        Assert.Throws<ArgumentException>(() => OperationResult.Fail(errorCode, "message"));
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
        Assert.Throws<ArgumentException>(() => OperationResult.Fail("CODE", errorMessage));
    }

    /// <summary>
    ///     Ok should create a successful result.
    /// </summary>
    [Fact]
    public void OkCreatesSuccessResult()
    {
        OperationResult result = OperationResult.Ok();
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }
}