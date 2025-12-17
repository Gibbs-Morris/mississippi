using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Root-level command handler that composes one or more <see cref="ICommandHandler{TState}" /> instances.
/// </summary>
/// <typeparam name="TState">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This class mirrors <c>RootReducer</c> for reducers, providing a consistent pattern
///         for dispatching commands to the appropriate handler at runtime.
///     </para>
///     <para>
///         Commands are dispatched by iterating through registered handlers and calling
///         <see cref="ICommandHandler{TState}.TryHandle" /> until one returns <c>true</c>.
///     </para>
/// </remarks>
public sealed class RootCommandHandler<TState> : IRootCommandHandler<TState>
{
    private readonly ICommandHandler<TState>[] handlers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootCommandHandler{TState}" /> class.
    /// </summary>
    /// <param name="handlers">The command handlers that can process commands for this state type.</param>
    /// <param name="logger">The logger used for command handler diagnostics.</param>
    public RootCommandHandler(
        IEnumerable<ICommandHandler<TState>> handlers,
        ILogger<RootCommandHandler<TState>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(handlers);
        this.handlers = handlers.ToArray();
        Logger = logger ?? NullLogger<RootCommandHandler<TState>>.Instance;
    }

    private ILogger<RootCommandHandler<TState>> Logger { get; }

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        object command,
        TState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        string stateType = typeof(TState).Name;
        string commandType = command.GetType().Name;
        Logger.RootCommandHandlerHandling(stateType, commandType);
        for (int i = 0; i < handlers.Length; i++)
        {
            ICommandHandler<TState> handler = handlers[i];
            if (handler.TryHandle(command, state, out OperationResult<IReadOnlyList<object>> result))
            {
                Logger.RootCommandHandlerHandlerMatched(handler.GetType().Name, commandType);
                return result;
            }
        }

        Logger.RootCommandHandlerNoHandlerMatched(stateType, commandType);
        return OperationResult.Fail<IReadOnlyList<object>>(
            AggregateErrorCodes.CommandHandlerNotFound,
            $"No command handler registered for command type {commandType}.");
    }
}