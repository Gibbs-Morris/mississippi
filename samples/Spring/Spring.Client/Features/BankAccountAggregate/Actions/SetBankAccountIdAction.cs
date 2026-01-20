using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to set the current bank account ID for targeting commands.
/// </summary>
/// <param name="BankAccountId">The bank account ID to set as current.</param>
internal sealed record SetBankAccountIdAction(string BankAccountId) : IAction;