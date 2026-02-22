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
///     Root-level command handler that composes one or more <see cref="ICommandHandler{TSnapshot}" /> instances.
/// </summary>
/// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
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
public sealed class RootCommandHandler<TSnapshot> : IRootCommandHandler<TSnapshot>
{
    private static readonly Type StateType = typeof(TSnapshot);

    private readonly ImmutableArray<ICommandHandler<TSnapshot>> fallbackHandlers;

    private readonly FrozenDictionary<Type, ImmutableArray<ICommandHandler<TSnapshot>>> handlerIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootCommandHandler{TSnapshot}" /> class.
    /// </summary>
    /// <param name="handlers">The command handlers that can process commands for this state type.</param>
    /// <param name="logger">The logger used for command handler diagnostics.</param>
    public RootCommandHandler(
        IEnumerable<ICommandHandler<TSnapshot>> handlers,
        ILogger<RootCommandHandler<TSnapshot>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ICommandHandler<TSnapshot>[] handlersArray = handlers.ToArray();
        Logger = logger ?? NullLogger<RootCommandHandler<TSnapshot>>.Instance;
        (handlerIndex, fallbackHandlers) = BuildHandlerIndex(handlersArray);
    }

    private ILogger<RootCommandHandler<TSnapshot>> Logger { get; }

    /// <summary>
    ///     Builds an index mapping command types to their handlers, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<ICommandHandler<TSnapshot>>> Index,
        ImmutableArray<ICommandHandler<TSnapshot>> Fallback) BuildHandlerIndex(
            ICommandHandler<TSnapshot>[] handlersArray
        )
    {
        Dictionary<Type, ImmutableArray<ICommandHandler<TSnapshot>>.Builder> indexBuilder = new();
        ImmutableArray<ICommandHandler<TSnapshot>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<ICommandHandler<TSnapshot>>();
        foreach (ICommandHandler<TSnapshot> handler in handlersArray)
        {
            Type? commandType = ExtractCommandType(handler.GetType());
            if (commandType is not null)
            {
                if (!indexBuilder.TryGetValue(
                        commandType,
                        out ImmutableArray<ICommandHandler<TSnapshot>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<ICommandHandler<TSnapshot>>();
                    indexBuilder[commandType] = list;
                }

                list.Add(handler);
            }
            else
            {
                fallbackBuilder.Add(handler);
            }
        }

        FrozenDictionary<Type, ImmutableArray<ICommandHandler<TSnapshot>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Extracts the TCommand type argument from a handler implementing ICommandHandler{TCommand, TSnapshot}.
    /// </summary>
    private static Type? ExtractCommandType(
        Type handlerType
    )
    {
        // Look for ICommandHandler<TCommand, TSnapshot> in the interface list.
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

            // typeArgs[0] = TCommand, typeArgs[1] = TSnapshot
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
        TSnapshot? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        string stateType = StateType.Name;
        Type commandRuntimeType = command.GetType();
        string commandType = commandRuntimeType.Name;
        Logger.RootCommandHandlerHandling(commandType, stateType);

        // Fast path: look up handlers registered for this exact command type.
        if (handlerIndex.TryGetValue(commandRuntimeType, out ImmutableArray<ICommandHandler<TSnapshot>> indexed))
        {
            foreach (ICommandHandler<TSnapshot> handler in indexed)
            {
                if (handler.TryHandle(command, state, out OperationResult<IReadOnlyList<object>> result))
                {
                    Logger.RootCommandHandlerHandlerMatched(handler.GetType().Name, commandType);
                    return result;
                }
            }
        }

        // Slow path: iterate fallback handlers whose command type could not be determined at construction.
        foreach (ICommandHandler<TSnapshot> handler in fallbackHandlers)
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