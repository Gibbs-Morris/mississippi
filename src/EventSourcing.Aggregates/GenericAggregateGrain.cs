using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Diagnostics;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Generic aggregate grain that processes commands for any aggregate type.
/// </summary>
/// <typeparam name="TAggregate">
///     The aggregate state type, decorated with
///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
/// </typeparam>
/// <remarks>
///     <para>
///         This grain eliminates the need for custom grain classes per aggregate. The grain is keyed
///         by entity ID only; the brook name is derived from the
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         on the <typeparamref name="TAggregate" /> type.
///     </para>
///     <para>
///         Commands are dispatched to registered <see cref="ICommandHandler{TSnapshot}" /> implementations
///         via the <see cref="IRootCommandHandler{TSnapshot}" /> at runtime.
///     </para>
///     <para>
///         The <typeparamref name="TAggregate" /> type MUST be decorated with
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
///         If the attribute is missing, the grain will fail to activate with an exception.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Aggregates.GenericAggregateGrain`1")]
internal sealed class GenericAggregateGrain<TAggregate>
    : IGenericAggregateGrain<TAggregate>,
      IGrainBase
    where TAggregate : class
{
    private BrookKey brookKey;

    /// <summary>
    ///     Tracks the last known position after the aggregate has written events.
    ///     <c>null</c> means the aggregate hasn't written yet and should query the cursor grain.
    /// </summary>
    private BrookPosition? lastKnownPosition;

    private SnapshotStreamKey snapshotStreamKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericAggregateGrain{TAggregate}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root event reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="rootEventEffect">
    ///     Optional root event effect dispatcher for running side effects after events are persisted.
    ///     When null, no effects are executed.
    /// </param>
    public GenericAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<TAggregate> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<TAggregate> rootReducer,
        ILogger<GenericAggregateGrain<TAggregate>> logger,
        IRootEventEffect<TAggregate>? rootEventEffect = null
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        BrookGrainFactory = brookGrainFactory ?? throw new ArgumentNullException(nameof(brookGrainFactory));
        BrookEventConverter = brookEventConverter ?? throw new ArgumentNullException(nameof(brookEventConverter));
        RootCommandHandler = rootCommandHandler ?? throw new ArgumentNullException(nameof(rootCommandHandler));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RootEventEffect = rootEventEffect;
    }

    /// <summary>
    ///     Gets the brook name from the
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     on the <typeparamref name="TAggregate" /> type.
    /// </summary>
    private static string BrookName => BrookNameHelper.GetBrookName<TAggregate>();

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IBrookEventConverter BrookEventConverter { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    private ILogger<GenericAggregateGrain<TAggregate>> Logger { get; }

    private IRootCommandHandler<TAggregate> RootCommandHandler { get; }

    private IRootEventEffect<TAggregate>? RootEventEffect { get; }

    private IRootReducer<TAggregate> RootReducer { get; }

    private ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <inheritdoc />
    public Task<OperationResult> ExecuteAsync(
        object command,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteInternalAsync(command, null, cancellationToken);

    /// <inheritdoc />
    public Task<OperationResult> ExecuteAsync(
        object command,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteInternalAsync(command, expectedVersion, cancellationToken);

    /// <inheritdoc />
    public async Task<TAggregate?> GetStateAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Get the latest brook position - use local cache if available to avoid race conditions
        BrookPosition currentPosition;
        if (lastKnownPosition.HasValue)
        {
            currentPosition = lastKnownPosition.Value;
        }
        else
        {
            currentPosition = await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
            lastKnownPosition = currentPosition;
        }

        if (currentPosition.NotSet)
        {
            return default;
        }

        SnapshotKey snapshotKey = new(snapshotStreamKey, currentPosition.Value);
        return await SnapshotGrainFactory.GetSnapshotCacheGrain<TAggregate>(snapshotKey)
            .GetStateAsync(cancellationToken);
    }

    /// <summary>
    ///     Called when the grain is activated. Initializes the brook key and snapshot stream key.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     is missing from the <typeparamref name="TAggregate" /> type.
    /// </exception>
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        // Validate that the attribute is present on the aggregate type (fail-fast)
        // This call will throw InvalidOperationException if the attribute is missing
        _ = BrookNameHelper.GetBrookName<TAggregate>();

        // Key is just entityId; brookKey includes the brook name for storage operations
        string entityId = this.GetPrimaryKeyString();
        brookKey = BrookKey.ForType<TAggregate>(entityId);
        snapshotStreamKey = new(
            BrookName,
            SnapshotStorageNameHelper.GetStorageName<TAggregate>(),
            entityId,
            RootReducer.GetReducerHash());
        Logger.Activated(brookKey);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Dispatches events to the root event effect and persists any yielded events.
    /// </summary>
    /// <param name="initialEvents">The initial events to dispatch.</param>
    /// <param name="currentState">The current aggregate state after events were applied.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    ///     Effects can yield additional events. These are persisted immediately for real-time
    ///     projection updates. The method loops until no more events are yielded or the
    ///     iteration limit (10) is reached to prevent infinite loops.
    /// </remarks>
    private async Task DispatchEffectsAsync(
        IReadOnlyList<object> initialEvents,
        TAggregate currentState,
        string aggregateKey,
        CancellationToken cancellationToken
    )
    {
        if (RootEventEffect is null)
        {
            return;
        }

        const int maxIterations = 10;
        List<object> pendingEvents = new(initialEvents);
        int iteration = 0;
        while ((pendingEvents.Count > 0) && (iteration < maxIterations))
        {
            iteration++;
            List<object> yieldedEvents = [];
            foreach (object eventData in pendingEvents)
            {
                await foreach (object resultEvent in RootEventEffect.DispatchAsync(
                                   eventData,
                                   currentState,
                                   cancellationToken))
                {
                    yieldedEvents.Add(resultEvent);

                    // Persist immediately for real-time projection updates
                    ImmutableArray<BrookEvent> brookEvents =
                        BrookEventConverter.ToStorageEvents(brookKey, [resultEvent]);
                    BrookPosition expectedPos = lastKnownPosition!.Value;
                    await BrookGrainFactory.GetBrookWriterGrain(brookKey)
                        .AppendEventsAsync(brookEvents, expectedPos, cancellationToken);
                    lastKnownPosition = new BrookPosition(expectedPos.Value + 1);
                }
            }

            pendingEvents = yieldedEvents;
        }

        if (iteration >= maxIterations)
        {
            Logger.EffectIterationLimitReached(aggregateKey, maxIterations);
            EventEffectMetrics.RecordIterationLimitReached(typeof(TAggregate).Name, aggregateKey);
        }
    }

    private async Task<OperationResult> ExecuteInternalAsync(
        object command,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        string commandTypeName = command.GetType().Name;
        string aggregateTypeName = typeof(TAggregate).Name;
        string aggregateKey = brookKey;
        Logger.CommandReceived(commandTypeName, aggregateKey);
        Stopwatch sw = Stopwatch.StartNew();

        // Get the latest brook position - use local cache if available to avoid race conditions
        BrookPosition currentPosition;
        if (lastKnownPosition.HasValue)
        {
            currentPosition = lastKnownPosition.Value;
        }
        else
        {
            currentPosition = await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
            lastKnownPosition = currentPosition;
        }

        // Check optimistic concurrency
        if (expectedVersion.HasValue && (expectedVersion.Value != currentPosition))
        {
            sw.Stop();
            string message =
                $"Expected version {expectedVersion.Value.Value} but current version is {currentPosition.Value}.";
            Logger.CommandFailed(commandTypeName, aggregateKey, AggregateErrorCodes.ConcurrencyConflict, message);
            AggregateMetrics.RecordConcurrencyConflict(aggregateTypeName);
            AggregateMetrics.RecordCommandFailure(
                aggregateTypeName,
                commandTypeName,
                sw.Elapsed.TotalMilliseconds,
                AggregateErrorCodes.ConcurrencyConflict);
            return OperationResult.Fail(AggregateErrorCodes.ConcurrencyConflict, message);
        }

        // Get current state from snapshot grain (single source of truth)
        Stopwatch stateFetchSw = Stopwatch.StartNew();
        TAggregate? state;
        if (currentPosition.NotSet)
        {
            // No events yet, use initial state (null - reducers/command handlers must handle null input)
            state = default;
        }
        else
        {
            SnapshotKey snapshotKey = new(snapshotStreamKey, currentPosition.Value);
            state = await SnapshotGrainFactory.GetSnapshotCacheGrain<TAggregate>(snapshotKey)
                .GetStateAsync(cancellationToken);
        }

        stateFetchSw.Stop();
        AggregateMetrics.RecordStateFetch(aggregateTypeName, stateFetchSw.Elapsed.TotalMilliseconds);

        // Delegate to root command handler
        OperationResult<IReadOnlyList<object>> handlerResult = RootCommandHandler.Handle(command, state);
        if (!handlerResult.Success)
        {
            sw.Stop();
            Logger.CommandFailed(commandTypeName, aggregateKey, handlerResult.ErrorCode, handlerResult.ErrorMessage);
            AggregateMetrics.RecordCommandFailure(
                aggregateTypeName,
                commandTypeName,
                sw.Elapsed.TotalMilliseconds,
                handlerResult.ErrorCode ?? "unknown");
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

            // Update local position to avoid race conditions with cursor grain stream updates
            lastKnownPosition = new BrookPosition(currentPosition.Value + brookEvents.Length);

            // Dispatch effects if any are registered
            if (RootEventEffect?.EffectCount > 0)
            {
                // Get updated state after events were applied for effect handlers
                SnapshotKey postEventSnapshotKey = new(snapshotStreamKey, lastKnownPosition.Value.Value);
                TAggregate? updatedState = await SnapshotGrainFactory
                    .GetSnapshotCacheGrain<TAggregate>(postEventSnapshotKey)
                    .GetStateAsync(cancellationToken);
                await DispatchEffectsAsync(events, updatedState!, aggregateKey, cancellationToken);
            }
        }

        sw.Stop();
        AggregateMetrics.RecordCommandSuccess(
            aggregateTypeName,
            commandTypeName,
            sw.Elapsed.TotalMilliseconds,
            events.Count);
        Logger.CommandExecuted(commandTypeName, aggregateKey);
        return OperationResult.Ok();
    }
}