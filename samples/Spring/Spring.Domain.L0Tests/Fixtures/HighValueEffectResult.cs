using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Testing.Effects;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Result from processing a deposit through the HighValueTransactionEffect.
/// </summary>
public sealed class HighValueEffectResult : EffectTestResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HighValueEffectResult" /> class.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="depositAmount">The deposit amount.</param>
    /// <param name="dispatchedCommands">The dispatched commands.</param>
    public HighValueEffectResult(
        string accountId,
        decimal depositAmount,
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> dispatchedCommands
    )
        : base(dispatchedCommands)
    {
        AccountId = accountId;
        DepositAmount = depositAmount;
    }

    /// <summary>
    ///     Gets the account ID that was processed.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    ///     Gets the deposit amount that was processed.
    /// </summary>
    public decimal DepositAmount { get; }

    /// <summary>
    ///     Gets a value indicating whether any commands were dispatched.
    /// </summary>
    public bool WasFlagged => HasDispatches;
}