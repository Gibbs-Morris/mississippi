using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched when the BankAccountBalance projection data is loaded.
/// </summary>
/// <param name="BankAccountBalanceId">The bank account balance ID.</param>
/// <param name="Balance">The current balance.</param>
/// <param name="HolderName">The account holder name.</param>
/// <param name="IsOpen">Whether the account is open.</param>
internal sealed record BankAccountBalanceLoadedAction(
    string BankAccountBalanceId,
    decimal Balance,
    string HolderName,
    bool IsOpen
) : IAction;