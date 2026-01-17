using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Api;

/// <summary>
///     Abstract base class for aggregate service implementations.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to provide a consistent service layer for aggregate
///         command execution. This base class provides:
///     </para>
///     <list type="bullet">
///         <item>Access to the aggregate grain factory.</item>
///         <item>Consistent logging for command execution.</item>
///         <item>Hooks for pre/post command execution logic.</item>
///     </list>
///     <para>
///         Generated service classes automatically inherit from this base when using
///         the <c>[AggregateService]</c> attribute with <c>GenerateApi = true</c>.
///     </para>
/// </remarks>
public abstract class AggregateServiceBase<TAggregate>
    where TAggregate : class
{
    private static readonly string AggregateTypeName = typeof(TAggregate).Name;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateServiceBase{TAggregate}" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    protected AggregateServiceBase(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<AggregateServiceBase<TAggregate>> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory ?? throw new ArgumentNullException(nameof(aggregateGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the factory for resolving aggregate grains.
    /// </summary>
    protected IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <summary>
    ///     Gets the logger for diagnostic output.
    /// </summary>
    protected ILogger<AggregateServiceBase<TAggregate>> Logger { get; }

    /// <summary>
    ///     Executes a command against the aggregate.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult" /> indicating success or failure.</returns>
    protected virtual async Task<OperationResult> ExecuteCommandAsync<TCommand>(
        string entityId,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(command);
        string commandType = typeof(TCommand).Name;
        Logger.ServiceExecutingCommand(entityId, commandType, AggregateTypeName);
        await OnBeforeExecuteAsync(entityId, command, cancellationToken);
        IGenericAggregateGrain<TAggregate> grain = AggregateGrainFactory.GetGenericAggregate<TAggregate>(entityId);
        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        await OnAfterExecuteAsync(entityId, command, result, cancellationToken);
        if (result.Success)
        {
            Logger.ServiceCommandSucceeded(entityId, commandType, AggregateTypeName);
        }
        else
        {
            Logger.ServiceCommandFailed(
                entityId,
                commandType,
                result.ErrorMessage ?? "Unknown error",
                AggregateTypeName);
        }

        return result;
    }

    /// <summary>
    ///     Called after a command is executed. Override to add post-execution logic
    ///     such as publishing events or triggering side effects.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command that was executed.</param>
    /// <param name="result">The operation result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnAfterExecuteAsync<TCommand>(
        string entityId,
        TCommand command,
        OperationResult result,
        CancellationToken cancellationToken = default
    )
        where TCommand : class =>
        Task.CompletedTask;

    /// <summary>
    ///     Called before a command is executed. Override to add pre-execution logic
    ///     such as validation or authorization checks.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnBeforeExecuteAsync<TCommand>(
        string entityId,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : class =>
        Task.CompletedTask;
}