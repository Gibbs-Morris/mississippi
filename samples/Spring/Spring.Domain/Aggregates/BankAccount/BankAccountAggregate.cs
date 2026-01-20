using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount;

/// <summary>
///     Internal aggregate state for the bank account.
///     This is never exposed externally; use projections for read queries.
/// </summary>
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTSTATE")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.BankAccountAggregate")]
public sealed record BankAccountAggregate
{
    /// <summary>
    ///     Gets the current balance of the account.
    /// </summary>
    [Id(0)]
    public decimal Balance { get; init; }

    /// <summary>
    ///     Gets the total number of deposits made to this account.
    /// </summary>
    [Id(3)]
    public int DepositCount { get; init; }

    /// <summary>
    ///     Gets the account holder name.
    /// </summary>
    [Id(2)]
    public string HolderName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the account has been opened.
    /// </summary>
    [Id(1)]
    public bool IsOpen { get; init; }

    /// <summary>
    ///     Gets the total number of withdrawals made from this account.
    /// </summary>
    [Id(4)]
    public int WithdrawalCount { get; init; }
}