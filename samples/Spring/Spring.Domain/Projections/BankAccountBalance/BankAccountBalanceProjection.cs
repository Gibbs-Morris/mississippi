using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Projections.BankAccountBalance;

/// <summary>
///     Read-optimized projection for the balance of a bank account.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a simple view of the current balance for a bank account.
///         It subscribes to events from the BankAccount aggregate: AccountOpened, FundsDeposited, FundsWithdrawn.
///     </para>
/// </remarks>
[ProjectionPath("bank-account-balance")]
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTBALANCE")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection")]
public sealed record BankAccountBalanceProjection
{
    /// <summary>
    ///     Gets the current balance of the account.
    /// </summary>
    [Id(0)]
    public decimal Balance { get; init; }

    /// <summary>
    ///     Gets the account holder name.
    /// </summary>
    [Id(1)]
    public string HolderName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the account is open.
    /// </summary>
    [Id(2)]
    public bool IsOpen { get; init; }
}