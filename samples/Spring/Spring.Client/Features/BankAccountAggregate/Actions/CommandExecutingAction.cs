using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a command starts executing.
/// </summary>
internal sealed record CommandExecutingAction : IAction;
