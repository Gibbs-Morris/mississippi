using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched when the BankAccountBalance projection data is loaded.
/// </summary>
/// <param name="AccountId">The account ID.</param>
/// <param name="Balance">The current balance.</param>
/// <param name="HolderName">The account holder name.</param>
/// <param name="IsOpen">Whether the account is open.</param>
internal sealed record BankAccountBalanceLoadedAction(
    string AccountId,
    decimal Balance,
    string HolderName,
    bool IsOpen) : IAction;
