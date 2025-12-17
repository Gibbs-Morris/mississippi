using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
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
///         Commands are dispatched using a precomputed type index built at construction time.
///         For each command type, only handlers registered for that exact type are considered,
///         preserving first-match-wins semantics within the original registration order.
///     </para>
/// </remarks>
public sealed class RootCommandHandler<TState> : IRootCommandHandler<TState>
{
    private static readonly Type StateType = typeof(TState);

    private readonly ImmutableArray<ICommandHandler<TState>> fallbackHandlers;

    private readonly FrozenDictionary<Type, ImmutableArray<ICommandHandler<TState>>> handlerIndex;

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
        ICommandHandler<TState>[] handlersArray = handlers.ToArray();
        Logger = logger ?? NullLogger<RootCommandHandler<TState>>.Instance;
        (handlerIndex, fallbackHandlers) = BuildHandlerIndex(handlersArray);
    }

    private ILogger<RootCommandHandler<TState>> Logger { get; }

    /// <summary>
    ///     Builds an index mapping command types to their handlers, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<ICommandHandler<TState>>> Index,
        ImmutableArray<ICommandHandler<TState>> Fallback) BuildHandlerIndex(
            ICommandHandler<TState>[] handlersArray
        )
    {
        Dictionary<Type, ImmutableArray<ICommandHandler<TState>>.Builder> indexBuilder = new();
        ImmutableArray<ICommandHandler<TState>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<ICommandHandler<TState>>();
        foreach (ICommandHandler<TState> handler in handlersArray)
        {
            Type? commandType = ExtractCommandType(handler.GetType());
            if (commandType is not null)
            {
                if (!indexBuilder.TryGetValue(commandType, out ImmutableArray<ICommandHandler<TState>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<ICommandHandler<TState>>();
                    indexBuilder[commandType] = list;
                }

                list.Add(handler);
            }
            else
            {
                fallbackBuilder.Add(handler);
            }
        }

        FrozenDictionary<Type, ImmutableArray<ICommandHandler<TState>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Extracts the TCommand type argument from a handler implementing ICommandHandler{TCommand, TState}.
    /// </summary>
    private static Type? ExtractCommandType(
        Type handlerType
    )
    {
        // Look for ICommandHandler<TCommand, TState> in the interface list.
        Type genericInterface = typeof(ICommandHandler<,>);
        foreach (Type iface in handlerType.GetInterfaces())
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            if (iface.GetGenericTypeDefinition() != genericInterface)
            {
                continue;
            }

            Type[] typeArgs = iface.GetGenericArguments();

            // typeArgs[0] = TCommand, typeArgs[1] = TState
            if ((typeArgs.Length == 2) && (typeArgs[1] == StateType))
            {
                return typeArgs[0];
            }
        }

        return null;
    }

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        object command,
        TState? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        string stateType = StateType.Name;
        Type commandRuntimeType = command.GetType();
        string commandType = commandRuntimeType.Name;
        Logger.RootCommandHandlerHandling(stateType, commandType);

        // Fast path: look up handlers registered for this exact command type.
        if (handlerIndex.TryGetValue(commandRuntimeType, out ImmutableArray<ICommandHandler<TState>> indexed))
        {
            foreach (ICommandHandler<TState> handler in indexed)
            {
                if (handler.TryHandle(command, state, out OperationResult<IReadOnlyList<object>> result))
                {
                    Logger.RootCommandHandlerHandlerMatched(handler.GetType().Name, commandType);
                    return result;
                }
            }
        }

        // Slow path: iterate fallback handlers whose command type could not be determined at construction.
        foreach (ICommandHandler<TState> handler in fallbackHandlers)
        {
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