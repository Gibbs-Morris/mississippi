using Spring.Domain.Aggregates.TransactionInvestigationQueue;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Fluent assertion extensions for <see cref="HighValueEffectResult" />.
/// </summary>
public static class EffectResultExtensions
{
    /// <summary>
    ///     Asserts that a FlagTransaction command was dispatched to the investigation queue.
    /// </summary>
    /// <param name="result">The effect result.</param>
    /// <param name="because">Optional reason.</param>
    /// <returns>The result for chaining.</returns>
    public static HighValueEffectResult ShouldHaveDispatchedFlagTransaction(
        this HighValueEffectResult result,
        string because = ""
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        because ??= string.Empty;
        result.WasFlagged.Should()
            .BeTrue(because.Length > 0 ? because : $"deposit of {result.DepositAmount:C} should exceed AML threshold");
        result.DispatchedCommands.Should().ContainSingle();
        result.DispatchedCommands[0].Command.Should().BeOfType<FlagTransaction>();
        result.DispatchedCommands[0].AggregateType.Should().Be<TransactionInvestigationQueueAggregate>();
        return result;
    }

    /// <summary>
    ///     Asserts that no commands were dispatched (deposit was below threshold).
    /// </summary>
    /// <param name="result">The effect result.</param>
    /// <param name="because">Optional reason.</param>
    /// <returns>The result for chaining.</returns>
    public static HighValueEffectResult ShouldNotHaveFlagged(
        this HighValueEffectResult result,
        string because = ""
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        because ??= string.Empty;
        result.WasFlagged.Should()
            .BeFalse(
                because.Length > 0 ? because : $"deposit of {result.DepositAmount:C} should not exceed AML threshold");
        return result;
    }

    /// <summary>
    ///     Asserts the FlagTransaction command contains the expected account ID.
    /// </summary>
    /// <param name="result">The effect result.</param>
    /// <param name="expectedAccountId">The expected account ID in the command.</param>
    /// <returns>The result for chaining.</returns>
    public static HighValueEffectResult WithAccountId(
        this HighValueEffectResult result,
        string expectedAccountId
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        result.DispatchedCommands.Should().ContainSingle();
        FlagTransaction command = result.DispatchedCommands[0].Command.Should().BeOfType<FlagTransaction>().Subject;
        command.AccountId.Should().Be(expectedAccountId);
        return result;
    }

    /// <summary>
    ///     Asserts the FlagTransaction command contains the expected amount.
    /// </summary>
    /// <param name="result">The effect result.</param>
    /// <param name="expectedAmount">The expected amount in the command.</param>
    /// <returns>The result for chaining.</returns>
    public static HighValueEffectResult WithAmount(
        this HighValueEffectResult result,
        decimal expectedAmount
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        result.DispatchedCommands.Should().ContainSingle();
        FlagTransaction command = result.DispatchedCommands[0].Command.Should().BeOfType<FlagTransaction>().Subject;
        command.Amount.Should().Be(expectedAmount);
        return result;
    }
}