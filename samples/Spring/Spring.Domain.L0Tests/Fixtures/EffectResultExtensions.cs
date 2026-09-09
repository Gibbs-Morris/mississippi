using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;


namespace MississippiSamples.Spring.Domain.L0Tests.Fixtures;

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
        Assert.True(
            result.WasFlagged,
            because.Length > 0 ? because : $"deposit of {result.DepositAmount:C} should exceed AML threshold");
        Assert.Single(result.DispatchedCommands);
        Assert.IsType<FlagTransaction>(result.DispatchedCommands[0].Command);
        Assert.Equal(typeof(TransactionInvestigationQueueAggregate), result.DispatchedCommands[0].AggregateType);
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
        Assert.False(
            result.WasFlagged,
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
        Assert.Single(result.DispatchedCommands);
        FlagTransaction command = Assert.IsType<FlagTransaction>(result.DispatchedCommands[0].Command);
        Assert.Equal(expectedAccountId, command.AccountId);
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
        Assert.Single(result.DispatchedCommands);
        FlagTransaction command = Assert.IsType<FlagTransaction>(result.DispatchedCommands[0].Command);
        Assert.Equal(expectedAmount, command.Amount);
        return result;
    }
}