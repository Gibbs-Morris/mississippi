using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides a base class for command handlers that process commands against aggregate state.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TState">The aggregate state type used for validation.</typeparam>
/// <remarks>
///     <para>
///         This base class provides the <see cref="TryHandle" /> implementation that enables
///         collection-based handler dispatch. Derived classes implement <see cref="HandleCore" />
///         with strongly-typed command and state parameters.
///     </para>
///     <para>
///         Handlers should be stateless and registered in the DI container.
///         They are discovered at aggregate activation time.
///     </para>
/// </remarks>
public abstract class CommandHandler<TCommand, TState> : ICommandHandler<TCommand, TState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        TCommand command,
        TState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        return HandleCore(command, state);
    }

    /// <inheritdoc />
    public bool TryHandle(
        object command,
        TState? state,
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

    /// <summary>
    ///     Handles the command by validating it against current state and producing domain events.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="state">The current aggregate state, or <c>default</c> if the aggregate is new.</param>
    /// <returns>
    ///     An <see cref="OperationResult{T}" /> containing the events to persist on success,
    ///     or an error code and message on failure.
    /// </returns>
    protected abstract OperationResult<IReadOnlyList<object>> HandleCore(
        TCommand command,
        TState? state
    );
}