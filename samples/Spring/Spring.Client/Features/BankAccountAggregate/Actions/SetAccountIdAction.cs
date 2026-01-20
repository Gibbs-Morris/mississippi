using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to set the current account ID for targeting commands.
/// </summary>
/// <param name="AccountId">The account ID to set as current.</param>
internal sealed record SetAccountIdAction(string AccountId) : IAction;
