using System;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Serialization;


namespace Mississippi.DomainModeling.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="OperationResult" /> behavior.
/// </summary>
public class OperationResultTests
{
    /// <summary>
    ///     Zero-initialized results represent success without error details.
    /// </summary>
    [Fact]
    public void DefaultResultRepresentsSuccess()
    {
        OperationResult result = default;
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.True(new OperationResult().Success);
        Assert.True((new OperationResult[1])[0].Success);
    }

    /// <summary>
    ///     Orleans transport preserves success and failure details, including zero-initialized results.
    /// </summary>
    [Fact]
    public void SerializationPreservesResultContracts()
    {
        ServiceCollection services = new();
        services.AddSerializer(builder => builder.AddAssembly(typeof(OperationResult).Assembly));
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Serializer serializer = serviceProvider.GetRequiredService<Serializer>();
        OperationResult[] results = [default, OperationResult.Ok(), OperationResult.Fail("CODE", "message")];
        foreach (OperationResult original in results)
        {
            OperationResult restored = serializer.Deserialize<OperationResult>(serializer.SerializeToArray(original));
            Assert.Equal(original.Success, restored.Success);
            Assert.Equal(original.ErrorCode, restored.ErrorCode);
            Assert.Equal(original.ErrorMessage, restored.ErrorMessage);
        }

        OperationResult<string>[] valueResults = [default, OperationResult.Ok("value"), OperationResult.Fail<string>("CODE", "message")];
        foreach (OperationResult<string> original in valueResults)
        {
            OperationResult<string> restored = serializer.Deserialize<OperationResult<string>>(serializer.SerializeToArray(original));
            Assert.Equal(original.Success, restored.Success);
            Assert.Equal(original.Value, restored.Value);
            Assert.Equal(original.ErrorCode, restored.ErrorCode);
            Assert.Equal(original.ErrorMessage, restored.ErrorMessage);
            Assert.Equal(original.Success, restored.ToResult().Success);
        }
    }


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
