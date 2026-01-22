using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a command starts executing.
/// </summary>
[PendingSourceGenerator]
internal sealed record CommandExecutingAction : IAction;