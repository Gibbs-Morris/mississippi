using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Command handler that wraps a delegate for processing commands.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
internal sealed class DelegateCommandHandler<TCommand, TSnapshot> : ICommandHandler<TCommand, TSnapshot>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DelegateCommandHandler{TCommand, TSnapshot}" /> class.
    /// </summary>
    /// <param name="handler">The delegate that handles the command.</param>
    public DelegateCommandHandler(
        Func<TCommand, TSnapshot?, OperationResult<IReadOnlyList<object>>> handler
    ) =>
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));

    private Func<TCommand, TSnapshot?, OperationResult<IReadOnlyList<object>>> Handler { get; }

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        TCommand command,
        TSnapshot? state
    ) =>
        Handler(command, state);

    /// <inheritdoc />
    public bool TryHandle(
        object command,
        TSnapshot? state,
        out OperationResult<IReadOnlyList<object>> result
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command is not TCommand typedCommand)
        {
            result = default!;
            return false;
        }

        result = Handle(typedCommand, state);
        return true;
    }
}