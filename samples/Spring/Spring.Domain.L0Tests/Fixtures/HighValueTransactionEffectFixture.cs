using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Effects;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.TransactionInvestigationQueue;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Simplified fixture for testing HighValueTransactionEffect with minimal boilerplate.
/// </summary>
/// <remarks>
///     <para>
///         Reduces the verbose effect test setup to fluent one-liners. Handles all mock wiring internally.
///     </para>
/// </remarks>
public static class HighValueTransactionEffectFixture
{
    /// <summary>
    ///     Processes a deposit through the effect and returns the dispatched commands.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="depositAmount">The deposit amount to test.</param>
    /// <param name="currentBalance">The account balance before the deposit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An effect result containing dispatched commands for assertions.</returns>
    public static async Task<HighValueEffectResult> ProcessDepositAsync(
        string accountId,
        decimal depositAmount,
        decimal currentBalance = 0m,
        CancellationToken cancellationToken = default
    )
    {
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey(accountId)
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
        HighValueTransactionEffect effect = harness.Build((
            factory,
            _,
            logger
        ) => new(factory, logger));
        FundsDeposited eventData = new()
        {
            Amount = depositAmount,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = currentBalance + depositAmount,
            IsOpen = true,
        };
        await harness.InvokeAsync(effect, eventData, state, cancellationToken);
        return new(accountId, depositAmount, harness.DispatchedCommands);
    }

    /// <summary>
    ///     Creates a harness for custom effect testing scenarios.
    /// </summary>
    /// <param name="accountId">The account ID for the grain context.</param>
    /// <returns>A configured effect test harness.</returns>
    internal static EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> CreateHarness(
        string accountId = "test-account"
    ) =>
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
            .WithGrainKey(accountId)
            .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
}