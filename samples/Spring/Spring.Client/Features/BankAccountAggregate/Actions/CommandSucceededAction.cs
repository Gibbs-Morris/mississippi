using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a command completes successfully.
/// </summary>
internal sealed record CommandSucceededAction : IAction;