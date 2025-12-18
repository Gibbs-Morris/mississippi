using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a command handler that attempts to process a command against aggregate state.
/// </summary>
/// <typeparam name="TSnapshot">The aggregate state type used for validation.</typeparam>
/// <remarks>
///     <para>
///         This non-generic base interface enables collection-based handler dispatch,
///         allowing aggregates to receive all registered handlers via constructor injection.
///         The <see cref="TryHandle" /> method uses runtime type checking to determine
///         if a handler can process a given command.
///     </para>
///     <para>
///         Implementations should inherit from <see cref="CommandHandler{TCommand, TSnapshot}" />
///         rather than implementing this interface directly.
///     </para>
/// </remarks>
public interface ICommandHandler<in TSnapshot>
{
    /// <summary>
    ///     Attempts to handle a command by validating it against current state and producing domain events.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="state">The current aggregate state, or <c>default</c> if the aggregate is new.</param>
    /// <param name="result">
    ///     When this method returns <c>true</c>, contains the operation result with events to persist
    ///     on success or an error on failure. When this method returns <c>false</c>, this value is undefined.
    /// </param>
    /// <returns>
    ///     <c>true</c> if this handler can process the command type; otherwise, <c>false</c>.
    /// </returns>
    bool TryHandle(
        object command,
        TSnapshot? state,
        out OperationResult<IReadOnlyList<object>> result
    );
}

/// <summary>
///     Defines a handler that processes a command against aggregate state and produces domain events.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TSnapshot">The aggregate state type used for validation.</typeparam>
/// <remarks>
///     <para>
///         Command handlers contain the business logic for validating commands and deciding
///         which events to emit. They receive the current aggregate state (which may be null
///         for new aggregates) and return either a success result with events or a failure result.
///     </para>
///     <para>
///         Handlers should be stateless and inherit from <see cref="CommandHandler{TCommand, TSnapshot}" />
///         rather than implementing this interface directly. Register handlers in the DI container
///         as <see cref="ICommandHandler{TSnapshot}" /> to enable collection-based dispatch.
///     </para>
/// </remarks>
public interface ICommandHandler<in TCommand, in TSnapshot> : ICommandHandler<TSnapshot>
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
        TSnapshot? state
    );
}