#if false // Replaced by source generator: CommandClientActionsGenerator
using System;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when an OpenAccount command fails.
/// </summary>
/// <param name="CommandId">The unique command invocation identifier.</param>
/// <param name="ErrorCode">The error code.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="Timestamp">The timestamp when the command failed.</param>
[PendingSourceGenerator]
internal sealed record OpenAccountFailedAction(
    string CommandId,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset Timestamp
) : ICommandFailedAction<OpenAccountFailedAction>
{
    /// <inheritdoc />
    public static OpenAccountFailedAction Create(
        string commandId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset timestamp
    ) =>
        new(commandId, errorCode, errorMessage, timestamp);
}
#endif