using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Action dispatched to set the demo account identifiers and names.
/// </summary>
/// <param name="AccountAId">The demo account A identifier.</param>
/// <param name="AccountAName">The demo account A display name.</param>
/// <param name="AccountBId">The demo account B identifier.</param>
/// <param name="AccountBName">The demo account B display name.</param>
internal sealed record SetDemoAccountsAction(
    string AccountAId,
    string AccountAName,
    string AccountBId,
    string AccountBName
) : IAction;