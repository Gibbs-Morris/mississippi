using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched when fetching the BankAccountBalance projection fails.
/// </summary>
/// <param name="ErrorMessage">The error message.</param>
internal sealed record BankAccountBalanceFetchFailedAction(string ErrorMessage) : IAction;