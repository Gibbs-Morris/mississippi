using System;
using System.Collections.Generic;
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
///     <example>
///         <code>
///         // Before (verbose)
///         var harness = EffectTestHarness&lt;HighValueTransactionEffect, FundsDeposited, BankAccountAggregate&gt;
///             .Create()
///             .WithGrainKey("acc-123")
///             .WithAggregateGrainResponse&lt;TransactionInvestigationQueueAggregate&gt;("global", OperationResult.Ok());
///         var effect = harness.Build((f, c, l) => new HighValueTransactionEffect(f, c, l));
///         await harness.InvokeAsync(effect, event, state);
///         harness.DispatchedCommands.ShouldHaveDispatched&lt;FlagTransaction&gt;();
///
///         // After (concise)
///         var result = await HighValueTransactionEffectFixture.ProcessDeposit("acc-123", 15_000m);
///         result.ShouldHaveDispatchedFlagTransaction();
///         </code>
///     </example>
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
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>
                .Create()
                .WithGrainKey(accountId)
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>(
                    "global",
                    OperationResult.Ok());

        HighValueTransactionEffect effect = harness.Build(
            (factory, context, logger) => new HighValueTransactionEffect(factory, context, logger));

        FundsDeposited eventData = new() { Amount = depositAmount };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = currentBalance + depositAmount,
            IsOpen = true,
        };

        await harness.InvokeAsync(effect, eventData, state, cancellationToken);

        return new HighValueEffectResult(accountId, depositAmount, harness.DispatchedCommands);
    }

    /// <summary>
    ///     Creates a harness for custom effect testing scenarios.
    /// </summary>
    /// <param name="accountId">The account ID for the grain context.</param>
    /// <returns>A configured effect test harness.</returns>
    internal static EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>
        CreateHarness(string accountId = "test-account") =>
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>
            .Create()
            .WithGrainKey(accountId)
            .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>(
                "global",
                OperationResult.Ok());
}
