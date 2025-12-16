using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a handler that processes a command against aggregate state and produces domain events.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TState">The aggregate state type used for validation.</typeparam>
/// <remarks>
///     <para>
///         Command handlers contain the business logic for validating commands and deciding
///         which events to emit. They receive the current aggregate state (which may be null
///         for new aggregates) and return either a success result with events or a failure result.
///     </para>
///     <para>
///         Handlers should be stateless and registered as transient services in the DI container.
///         They are resolved per command execution.
///     </para>
/// </remarks>
public interface ICommandHandler<in TCommand, in TState>
{
    /// <summary>
    ///     Handles the command by validating it against current state and producing domain events.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="state">The current aggregate state, or <c>default</c> if the aggregate is new.</param>
    /// <returns>
    ///     An <see cref="OperationResult{T}" /> containing the events to persist on success,
    ///     or an error code and message on failure.
    /// </returns>
    OperationResult<IReadOnlyList<object>> Handle(
        TCommand command,
        TState? state
    );
}