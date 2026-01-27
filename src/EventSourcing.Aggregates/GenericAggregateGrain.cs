using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// <param name="grainFactory">The Orleans grain factory for resolving grains.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root event reducer for obtaining the reducers hash.</param>
    /// <param name="effectOptions">Options controlling aggregate effect processing.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="fireAndForgetEffectRegistrations">
    ///     Registrations for fire-and-forget effects that run in separate worker grains.
    /// </param>
    /// <param name="rootEventEffect">
    ///     Optional root event effect dispatcher for running side effects after events are persisted.
    ///     When null, no effects are executed.
    /// </param>
    public GenericAggregateGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<TAggregate> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<TAggregate> rootReducer,
        IOptions<AggregateEffectOptions> effectOptions,
        ILogger<GenericAggregateGrain<TAggregate>> logger,
        IEnumerable<IFireAndForgetEffectRegistration<TAggregate>> fireAndForgetEffectRegistrations,
        IRootEventEffect<TAggregate>? rootEventEffect = null
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        BrookGrainFactory = brookGrainFactory ?? throw new ArgumentNullException(nameof(brookGrainFactory));
        BrookEventConverter = brookEventConverter ?? throw new ArgumentNullException(nameof(brookEventConverter));
        RootCommandHandler = rootCommandHandler ?? throw new ArgumentNullException(nameof(rootCommandHandler));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        EffectOptions = effectOptions?.Value ?? throw new ArgumentNullException(nameof(effectOptions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FireAndForgetEffectRegistrations = fireAndForgetEffectRegistrations?.ToArray() ?? [];
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

    private AggregateEffectOptions EffectOptions { get; }

    private IFireAndForgetEffectRegistration<TAggregate>[] FireAndForgetEffectRegistrations { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<GenericAggregateGrain<TAggregate>> Logger { get; }

    private IRootCommandHandler<TAggregate> RootCommandHandler { get; }

    private IRootEventEffect<TAggregate>? RootEventEffect { get; }

    private IRootReducer<TAggregate> RootReducer { get; }

    private ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <summary>
    ///     Determines if an exception is critical and should not be swallowed.
    /// </summary>
    /// <remarks>
    ///     Critical exceptions indicate catastrophic failures that should propagate
    ///     rather than being silently swallowed. These include memory exhaustion,
    ///     stack overflow, and thread abort conditions.
    /// </remarks>
    private static bool IsCriticalException(
        Exception ex
    ) =>
        ex is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;

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
    ///     <para>
    ///         Effects can yield additional events. These are persisted immediately for real-time
    ///         projection updates. The method loops until no more events are yielded or the
    ///         iteration limit (<see cref="AggregateEffectOptions.MaxEffectIterations" />) is reached
    ///         to prevent infinite loops.
    ///     </para>
    ///     <para>
    ///         When the limit is reached, remaining pending events are not processed, and a warning
    ///         is logged with metrics recorded. This may indicate a design issue with effects
    ///         continuously producing events in a cycle.
    ///     </para>
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

        int maxIterations = EffectOptions.MaxEffectIterations;
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
                                   brookKey,
                                   lastKnownPosition?.Value ?? 0,
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

                    // Update state after each yielded event so subsequent effects see the correct state
                    SnapshotKey updatedSnapshotKey = new(snapshotStreamKey, lastKnownPosition.Value.Value);
                    TAggregate? updatedState = await SnapshotGrainFactory
                        .GetSnapshotCacheGrain<TAggregate>(updatedSnapshotKey)
                        .GetStateAsync(cancellationToken);
                    if (updatedState is not null)
                    {
                        currentState = updatedState;
                    }
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

    /// <summary>
    ///     Dispatches fire-and-forget effects for the given events.
    /// </summary>
    /// <param name="events">The events to dispatch effects for.</param>
    /// <param name="currentState">The current aggregate state after events were applied.</param>
    /// <param name="startingPosition">The brook position of the first event in the list.</param>
    /// <remarks>
    ///     <para>
    ///         Fire-and-forget effects run in separate <c>[StatelessWorker]</c> grains using
    ///         <c>[OneWay]</c> semantics. This method dispatches them without waiting for completion.
    ///     </para>
    ///     <para>
    ///         Unlike synchronous effects, fire-and-forget effects cannot yield additional events.
    ///         They are designed for side effects like sending notifications, updating external systems,
    ///         or triggering asynchronous workflows.
    ///     </para>
    /// </remarks>
    private void DispatchFireAndForgetEffects(
        IReadOnlyList<object> events,
        TAggregate currentState,
        long startingPosition
    )
    {
        if (FireAndForgetEffectRegistrations.Length == 0)
        {
            return;
        }

        for (int i = 0; i < events.Count; i++)
        {
            object eventData = events[i];
            long eventPosition = startingPosition + i;
            Type eventType = eventData.GetType();
            foreach (IFireAndForgetEffectRegistration<TAggregate> registration in FireAndForgetEffectRegistrations)
            {
                if (registration.EventType == eventType)
                {
                    registration.Dispatch(GrainFactory, eventData, currentState, brookKey, eventPosition);
                    FireAndForgetEffectMetrics.RecordDispatch(
                        typeof(TAggregate).Name,
                        eventType.Name,
                        registration.EffectTypeName);
                }
            }
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

        BrookPosition currentPosition = await GetCurrentPositionAsync();

        OperationResult? concurrencyError = CheckForConcurrencyConflict(expectedVersion, currentPosition);
        if (concurrencyError.HasValue)
        {
            RecordConcurrencyConflict(
                expectedVersion!.Value,
                currentPosition,
                commandTypeName,
                aggregateTypeName,
                aggregateKey,
                sw);
            return concurrencyError.Value;
        }

        TAggregate? state = await FetchCurrentStateAsync(currentPosition, aggregateTypeName, cancellationToken);

        OperationResult<IReadOnlyList<object>> handlerResult = RootCommandHandler.Handle(command, state);
        if (!handlerResult.Success)
        {
            return RecordCommandFailure(
                handlerResult,
                commandTypeName,
                aggregateTypeName,
                aggregateKey,
                sw);
        }

        IReadOnlyList<object> events = handlerResult.Value;
        if (events.Count > 0)
        {
            await PersistEventsAndDispatchEffectsAsync(
                events,
                currentPosition,
                commandTypeName,
                aggregateKey,
                cancellationToken);
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

    private async Task<BrookPosition> GetCurrentPositionAsync()
    {
        if (lastKnownPosition.HasValue)
        {
            return lastKnownPosition.Value;
        }

        BrookPosition position = await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
        lastKnownPosition = position;
        return position;
    }

    private static OperationResult? CheckForConcurrencyConflict(
        BrookPosition? expectedVersion,
        BrookPosition currentPosition
    )
    {
        if (!expectedVersion.HasValue || (expectedVersion.Value == currentPosition))
        {
            return null;
        }

        string message =
            $"Expected version {expectedVersion.Value.Value} but current version is {currentPosition.Value}.";
        return OperationResult.Fail(AggregateErrorCodes.ConcurrencyConflict, message);
    }

    private void RecordConcurrencyConflict(
        BrookPosition expectedVersion,
        BrookPosition currentPosition,
        string commandTypeName,
        string aggregateTypeName,
        string aggregateKey,
        Stopwatch sw
    )
    {
        sw.Stop();
        string message = $"Expected version {expectedVersion.Value} but current version is {currentPosition.Value}.";
        Logger.CommandFailed(commandTypeName, aggregateKey, AggregateErrorCodes.ConcurrencyConflict, message);
        AggregateMetrics.RecordConcurrencyConflict(aggregateTypeName);
        AggregateMetrics.RecordCommandFailure(
            aggregateTypeName,
            commandTypeName,
            sw.Elapsed.TotalMilliseconds,
            AggregateErrorCodes.ConcurrencyConflict);
    }

    private async Task<TAggregate?> FetchCurrentStateAsync(
        BrookPosition currentPosition,
        string aggregateTypeName,
        CancellationToken cancellationToken
    )
    {
        Stopwatch stateFetchSw = Stopwatch.StartNew();
        TAggregate? state;

        if (currentPosition.NotSet)
        {
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
        return state;
    }

    private OperationResult RecordCommandFailure(
        OperationResult<IReadOnlyList<object>> handlerResult,
        string commandTypeName,
        string aggregateTypeName,
        string aggregateKey,
        Stopwatch sw
    )
    {
        sw.Stop();
        Logger.CommandFailed(
            commandTypeName,
            aggregateKey,
            handlerResult.ErrorCode ?? "unknown",
            handlerResult.ErrorMessage ?? "No error message provided");
        AggregateMetrics.RecordCommandFailure(
            aggregateTypeName,
            commandTypeName,
            sw.Elapsed.TotalMilliseconds,
            handlerResult.ErrorCode ?? "unknown");
        return handlerResult.ToResult();
    }

    private async Task PersistEventsAndDispatchEffectsAsync(
        IReadOnlyList<object> events,
        BrookPosition currentPosition,
        string commandTypeName,
        string aggregateKey,
        CancellationToken cancellationToken
    )
    {
        ImmutableArray<BrookEvent> brookEvents = BrookEventConverter.ToStorageEvents(brookKey, events);
        BrookPosition? expectedCursorPosition = currentPosition.NotSet ? null : currentPosition;

        await BrookGrainFactory.GetBrookWriterGrain(brookKey)
            .AppendEventsAsync(brookEvents, expectedCursorPosition, cancellationToken);

        lastKnownPosition = new BrookPosition(currentPosition.Value + brookEvents.Length);

        await DispatchSynchronousEffectsAsync(events, commandTypeName, aggregateKey, cancellationToken);
        await DispatchFireAndForgetEffectsIfRegisteredAsync(events, currentPosition, cancellationToken);
    }

    private async Task DispatchSynchronousEffectsAsync(
        IReadOnlyList<object> events,
        string commandTypeName,
        string aggregateKey,
        CancellationToken cancellationToken
    )
    {
        if (RootEventEffect?.EffectCount <= 0)
        {
            return;
        }

        SnapshotKey postEventSnapshotKey = new(snapshotStreamKey, lastKnownPosition!.Value.Value);
        TAggregate? updatedState = await SnapshotGrainFactory
            .GetSnapshotCacheGrain<TAggregate>(postEventSnapshotKey)
            .GetStateAsync(cancellationToken);

        try
        {
            await DispatchEffectsAsync(events, updatedState!, aggregateKey, cancellationToken);
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            Logger.EffectDispatchFailed(commandTypeName, aggregateKey, ex);
        }
    }

    private async Task DispatchFireAndForgetEffectsIfRegisteredAsync(
        IReadOnlyList<object> events,
        BrookPosition currentPosition,
        CancellationToken cancellationToken
    )
    {
        if (FireAndForgetEffectRegistrations.Length == 0)
        {
            return;
        }

        SnapshotKey postEventSnapshotKey = new(snapshotStreamKey, lastKnownPosition!.Value.Value);
        TAggregate? updatedState = await SnapshotGrainFactory
            .GetSnapshotCacheGrain<TAggregate>(postEventSnapshotKey)
            .GetStateAsync(cancellationToken);

        long startingPosition = currentPosition.Value + 1;
        DispatchFireAndForgetEffects(events, updatedState!, startingPosition);
    }
}