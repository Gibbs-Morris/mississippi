using System;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates invariant breaks caused by default-initialized <see cref="OperationResult" /> values.
/// </summary>
public sealed class OperationResultDefaultValueTests
{
    /// <summary>
    ///     default(OperationResult) produces a failed result with null error details, violating
    ///     the implied invariant that failed results include error code and message.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(OperationResult) bypasses factory methods and yields Success=false with null " +
        "ErrorCode/ErrorMessage, violating the failed-result invariant implied by the API and annotations.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/OperationResult.cs",
        LineNumbers = "54-55",
        Severity = "Medium",
        Category = "MissingValidation")]
    public void DefaultOperationResultHasFailureStateWithoutErrorDetails()
    {
        // Act
        OperationResult result = default;

        // Assert - failed state lacks required failure details
        Assert.False(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     default(OperationResult{T}) followed by ToResult throws because ToResult assumes failed
    ///     results always carry non-null error details.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "OperationResult<T>.ToResult assumes failed results have non-null ErrorCode and ErrorMessage. " +
        "default(OperationResult<T>) violates that assumption and ToResult throws ArgumentNullException.",
        FilePath = "src/EventSourcing.Aggregates.Abstractions/OperationResult.cs",
        LineNumbers = "158-159,195",
        Severity = "Medium",
        Category = "LogicError")]
    public void DefaultGenericOperationResultToResultThrows()
    {
        // Arrange
        OperationResult<int> result = default;

        // Act & Assert - conversion throws instead of returning a valid failed result
        Assert.Throws<ArgumentNullException>(() => result.ToResult());
    }
}
