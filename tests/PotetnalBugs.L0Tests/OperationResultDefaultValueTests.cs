using System;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that default-initialized <see cref="OperationResult" /> invariant breaks are now fixed
///     via IsDefault normalization in the Success property getter.
/// </summary>
public sealed class OperationResultDefaultValueTests
{
    /// <summary>
    ///     FIXED: default(OperationResult) is now treated as a successful result via IsDefault check.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(OperationResult) previously yielded Success=false with null ErrorCode/ErrorMessage. " +
        "The Success property now checks IsDefault and treats default-initialized values as successful.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/OperationResult.cs",
        LineNumbers = "54-55",
        Severity = "Medium",
        Category = "MissingValidation")]
    public void DefaultOperationResultIsTreatedAsSuccess()
    {
        // Act
        OperationResult result = default;

        // Assert - default is now treated as a success
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     FIXED: default(OperationResult{T}).ToResult() now returns Ok() instead of throwing,
    ///     because ToResult checks IsDefault before accessing error details.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: OperationResult<T>.ToResult previously threw ArgumentNullException for " +
        "default-initialized values. ToResult now checks IsDefault and returns OperationResult.Ok().",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/OperationResult.cs",
        LineNumbers = "158-159,195",
        Severity = "Medium",
        Category = "LogicError")]
    public void DefaultGenericOperationResultToResultReturnsSuccess()
    {
        // Arrange
        OperationResult<int> result = default;

        // Act - conversion no longer throws
        OperationResult converted = result.ToResult();

        // Assert - correctly treated as success
        Assert.True(converted.Success);
    }
}
