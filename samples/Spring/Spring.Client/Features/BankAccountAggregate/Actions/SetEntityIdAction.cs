using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to set the current entity ID for targeting commands.
/// </summary>
/// <param name="EntityId">The entity ID to set as current.</param>
[PendingSourceGenerator]
internal sealed record SetEntityIdAction(string EntityId) : IAction;