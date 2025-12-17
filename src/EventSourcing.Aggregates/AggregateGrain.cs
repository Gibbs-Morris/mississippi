using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Base class for aggregate grains that encapsulate domain state and process commands.
/// </summary>
/// <typeparam name="TState">The internal state type of the aggregate.</typeparam>
/// <typeparam name="TBrook">The brook definition type that identifies the event stream.</typeparam>
/// <remarks>
///     <para>
///         Aggregate grains are single-threaded write processors backed by a brook (event stream).
///         They hydrate state from the brook on activation, accept commands, validate against
///         current state, emit events, and persist those events back to the brook.
///     </para>
///     <para>
///         The internal state is never exposed; use projections for read queries.
///     </para>
///     <para>
///         The <typeparamref name="TBrook" /> type parameter provides compile-time type safety
///         for brook identity, ensuring projections can reference the same brook type.
///     </para>
/// </remarks>
public abstract class AggregateGrain<TState, TBrook>
    : IAggregateGrain,
      IGrainBase
    where TBrook : IBrookDefinition
{
    private BrookKey brookKey;

    private SnapshotStreamKey snapshotStreamKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateGrain{TState, TBrook}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="reducersHash">The hash of the reducers for snapshot versioning.</param>
    /// <param name="logger">Logger instance.</param>
    protected AggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<TState> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        string reducersHash,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        BrookGrainFactory = brookGrainFactory ?? throw new ArgumentNullException(nameof(brookGrainFactory));
        BrookEventConverter = brookEventConverter ?? throw new ArgumentNullException(nameof(brookEventConverter));
        RootCommandHandler = rootCommandHandler ?? throw new ArgumentNullException(nameof(rootCommandHandler));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        ReducersHash = reducersHash ?? throw new ArgumentNullException(nameof(reducersHash));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the brook name from the <typeparamref name="TBrook" /> definition.
    /// </summary>
    protected static string BrookName => TBrook.BrookName;

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the converter for transforming domain events to/from brook events.
    /// </summary>
    protected IBrookEventConverter BrookEventConverter { get; }

    /// <summary>
    ///     Gets the factory for resolving brook grains.
    /// </summary>
    protected IBrookGrainFactory BrookGrainFactory { get; }

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Gets the hash of the reducers for snapshot versioning.
    /// </summary>
    protected string ReducersHash { get; }

    /// <summary>
    ///     Gets the root command handler for processing commands.
    /// </summary>
    protected IRootCommandHandler<TState> RootCommandHandler { get; }

    /// <summary>
    ///     Gets the factory for resolving snapshot grains.
    /// </summary>
    protected ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <summary>
    ///     Called when the grain is activated. Initializes the brook key and snapshot stream key.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public virtual Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        brookKey = BrookKey.FromString(primaryKey);
        snapshotStreamKey = new(typeof(TState).Name, brookKey.Id, ReducersHash);
        Logger.Activated(primaryKey);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Creates the initial state when no snapshot or events exist.
    /// </summary>
    /// <returns>The initial state, or <c>null</c> if the reducer handles null input.</returns>
    protected virtual TState? CreateInitialState() => default;

    /// <summary>
    ///     Executes a command against the aggregate, producing and persisting events.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedVersion">
    ///     Optional expected version for optimistic concurrency control.
    ///     If provided and the current version doesn't match, the operation fails.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="OperationResult" /> indicating success or failure.</returns>
    protected async Task<OperationResult> ExecuteAsync<TCommand>(
        TCommand command,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        string commandTypeName = typeof(TCommand).Name;
        string aggregateKey = brookKey;
        Logger.CommandReceived(commandTypeName, aggregateKey);

        // Get the latest brook position
        BrookPosition currentPosition = await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();

        // Check optimistic concurrency
        if (expectedVersion.HasValue && (expectedVersion.Value != currentPosition))
        {
            string message =
                $"Expected version {expectedVersion.Value.Value} but current version is {currentPosition.Value}.";
            Logger.CommandFailed(commandTypeName, aggregateKey, AggregateErrorCodes.ConcurrencyConflict, message);
            return OperationResult.Fail(AggregateErrorCodes.ConcurrencyConflict, message);
        }

        // Get current state from snapshot grain (single source of truth)
        TState? state;
        if (currentPosition.NotSet)
        {
            // No events yet, use initial state
            state = CreateInitialState();
        }
        else
        {
            SnapshotKey snapshotKey = new(snapshotStreamKey, currentPosition.Value);
            state = await SnapshotGrainFactory.GetSnapshotCacheGrain<TState>(snapshotKey)
                .GetStateAsync(cancellationToken);
        }

        // Delegate to root command handler
        OperationResult<IReadOnlyList<object>> handlerResult = RootCommandHandler.Handle(command, state);
        if (!handlerResult.Success)
        {
            Logger.CommandFailed(commandTypeName, aggregateKey, handlerResult.ErrorCode, handlerResult.ErrorMessage);
            return handlerResult.ToResult();
        }

        // Persist events if any were produced
        IReadOnlyList<object> events = handlerResult.Value;
        if (events.Count > 0)
        {
            ImmutableArray<BrookEvent> brookEvents = BrookEventConverter.ToStorageEvents(brookKey, events);

            // Pass null for first write, otherwise pass current position for optimistic concurrency
            BrookPosition? expectedCursorPosition = currentPosition.NotSet ? null : currentPosition;
            await BrookGrainFactory.GetBrookWriterGrain(brookKey)
                .AppendEventsAsync(brookEvents, expectedCursorPosition, cancellationToken);
        }

        Logger.CommandExecuted(commandTypeName, aggregateKey);
        return OperationResult.Ok();
    }
}