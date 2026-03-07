using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when a bank account is opened.
/// </summary>
[EventStorageName("SPRING", "BANKING", "ACCOUNTOPENED")]
[GenerateSerializer]
[Alias("Mississippi.Spring.Domain.Aggregates.BankAccount.Events.AccountOpened")]
internal sealed record AccountOpened
{
    /// <summary>
    ///     Gets the name of the account holder.
    /// </summary>
    [Id(0)]
    public string HolderName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the initial deposit amount.
    /// </summary>
    [Id(1)]
    public decimal InitialDeposit { get; init; }
}