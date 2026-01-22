using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a command fails.
/// </summary>
/// <param name="ErrorCode">The error code.</param>
/// <param name="ErrorMessage">The error message.</param>
[PendingSourceGenerator]
internal sealed record CommandFailedAction(string? ErrorCode, string? ErrorMessage) : IAction;