using System;

using Orleans;


namespace Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;

/// <summary>
///     Command to flag a high-value transaction for manual investigation.
/// </summary>
/// <remarks>
///     <para>
///         This command is typically dispatched by <see cref="BankAccount.Effects.HighValueTransactionEffect" />
///         when a deposit exceeds the AML threshold (£10,000).
///     </para>
///     <para>
///         This command is internal (no <c>[GenerateCommand]</c> attribute) because it is
///         dispatched from a server-side effect, not exposed to clients via API endpoints.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Spring.Domain.TransactionInvestigationQueue.Commands.FlagTransaction")]
public sealed record FlagTransaction
{
    /// <summary>
    ///     Gets the identifier of the bank account that received the deposit.
    /// </summary>
    [Id(0)]
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the amount that was deposited.
    /// </summary>
    [Id(1)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the timestamp when the original deposit occurred.
    /// </summary>
    [Id(2)]
    public DateTimeOffset Timestamp { get; init; }
}