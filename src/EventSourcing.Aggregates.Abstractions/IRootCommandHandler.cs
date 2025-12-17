using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Interface for root-level command handlers that process commands against aggregate state.
///     Provides a method for handling commands and producing domain events.
/// </summary>
/// <typeparam name="TState">The aggregate state type used for validation.</typeparam>
/// <remarks>
///     <para>
///         The root command handler composes one or more <see cref="ICommandHandler{TState}" /> instances
///         and dispatches commands to the appropriate handler at runtime.
///     </para>
///     <para>
///         This pattern mirrors <c>IRootReducer</c> for reducers, providing a consistent
///         abstraction for command dispatch across aggregates.
///     </para>
/// </remarks>
public interface IRootCommandHandler<in TState>
{
    /// <summary>
    ///     Handles a command by validating it against current state and producing domain events.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="state">The current aggregate state, or <c>default</c> if the aggregate is new.</param>
    /// <returns>
    ///     An <see cref="OperationResult{T}" /> containing the events to persist on success,
    ///     or an error code and message on failure.
    /// </returns>
    OperationResult<IReadOnlyList<object>> Handle(
        object command,
        TState? state
    );
}