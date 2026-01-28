namespace Mississippi.EventSourcing.Sagas.Abstractions.Commands;

/// <summary>
///     Internal command to execute the next step in a saga.
/// </summary>
/// <remarks>
///     This command is dispatched by saga effects after step completion
///     to continue the saga workflow. It is not intended for external use.
/// </remarks>
public sealed record ExecuteNextStepCommand;