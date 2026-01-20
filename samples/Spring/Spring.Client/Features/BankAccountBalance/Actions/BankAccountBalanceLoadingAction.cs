using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched when the BankAccountBalance fetch starts.
/// </summary>
internal sealed record BankAccountBalanceLoadingAction : IAction;