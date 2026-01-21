using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a command fails.
/// </summary>
/// <param name="ErrorCode">The error code.</param>
/// <param name="ErrorMessage">The error message.</param>
internal sealed record CommandFailedAction(string? ErrorCode, string? ErrorMessage) : IAction;