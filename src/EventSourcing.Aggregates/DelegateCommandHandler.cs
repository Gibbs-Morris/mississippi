using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Command handler that wraps a delegate for processing commands.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TState">The aggregate state type.</typeparam>
internal sealed class DelegateCommandHandler<TCommand, TState> : ICommandHandler<TCommand, TState>
{
    private readonly Func<TCommand, TState?, OperationResult<IReadOnlyList<object>>> handler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DelegateCommandHandler{TCommand, TState}" /> class.
    /// </summary>
    /// <param name="handler">The delegate that handles the command.</param>
    public DelegateCommandHandler(
        Func<TCommand, TState?, OperationResult<IReadOnlyList<object>>> handler
    ) =>
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        TCommand command,
        TState? state
    ) =>
        handler(command, state);
}