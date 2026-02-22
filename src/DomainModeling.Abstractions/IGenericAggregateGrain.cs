using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     A generic aggregate grain interface that executes commands against an aggregate.
/// </summary>
/// <typeparam name="TAggregate">
///     The aggregate state type, decorated with
///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
/// </typeparam>
/// <remarks>
///     <para>
///         This generic grain interface eliminates the need for custom grain interfaces per aggregate.
///         The grain is keyed by entity ID only; the brook name is derived from the
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         on the <typeparamref name="TAggregate" /> type.
///     </para>
///     <para>
///         Commands are dispatched to registered <see cref="ICommandHandler{TSnapshot}" /> implementations
///         via the <see cref="IRootCommandHandler{TSnapshot}" /> at runtime.
///     </para>
///     <para>
///         For strongly-typed command execution, use the source-generated aggregate service layer
///         which wraps this grain with typed methods per command handler.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Aggregates.IGenericAggregateGrain`1")]
public interface IGenericAggregateGrain<TAggregate> : IGrainWithStringKey
    where TAggregate : class
{
    /// <summary>
    ///     Executes a command against the aggregate.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="OperationResult" /> indicating success or failure.
    /// </returns>
    /// <remarks>
    ///     The command is dispatched to the appropriate <see cref="ICommandHandler{TSnapshot}" />
    ///     based on the command's runtime type. If no handler is registered for the command type,
    ///     the operation fails with <see cref="AggregateErrorCodes.CommandHandlerNotFound" />.
    /// </remarks>
    [Alias("ExecuteAsync")]
    Task<OperationResult> ExecuteAsync(
        object command,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Executes a command against the aggregate with optimistic concurrency control.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedVersion">
    ///     The expected brook position for optimistic concurrency control.
    ///     If provided and the current version doesn't match, the operation fails.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="OperationResult" /> indicating success or failure.
    /// </returns>
    [Alias("ExecuteWithVersionAsync")]
    Task<OperationResult> ExecuteAsync(
        object command,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the current aggregate state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     The current aggregate state, or <c>null</c> if no events have been applied.
    /// </returns>
    /// <remarks>
    ///     This method retrieves the aggregate state by loading the latest snapshot and applying
    ///     any newer events. The state is returned for read-only purposes (e.g., for queries or
    ///     command validation that requires the full state).
    /// </remarks>
    [Alias("GetStateAsync")]
    Task<TAggregate?> GetStateAsync(
        CancellationToken cancellationToken = default
    );
}