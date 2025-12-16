using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;

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

    private BrookPosition currentPosition = new();

    private TState? state;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateGrain{TState, TBrook}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="serializationProvider">Provider for event serialization.</param>
    /// <param name="rootReducer">The root reducer for computing state from events.</param>
    /// <param name="logger">Logger instance.</param>
    protected AggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        ISerializationProvider serializationProvider,
        IRootReducer<TState> rootReducer,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        BrookGrainFactory = brookGrainFactory ?? throw new ArgumentNullException(nameof(brookGrainFactory));
        SerializationProvider = serializationProvider ?? throw new ArgumentNullException(nameof(serializationProvider));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the brook name from the <typeparamref name="TBrook" /> definition.
    /// </summary>
    protected static string BrookName => TBrook.BrookName;

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the factory for resolving brook grains.
    /// </summary>
    protected IBrookGrainFactory BrookGrainFactory { get; }

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Gets the root reducer for computing state from events.
    /// </summary>
    protected IRootReducer<TState> RootReducer { get; }

    /// <summary>
    ///     Gets the serialization provider for event serialization.
    /// </summary>
    protected ISerializationProvider SerializationProvider { get; }

    /// <summary>
    ///     Gets the service provider for resolving command handlers.
    /// </summary>
    protected IServiceProvider ServiceProvider => GrainContext.ActivationServices;

    /// <summary>
    ///     Called when the grain is activated. Hydrates state from the brook.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public virtual async Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        brookKey = BrookKey.FromString(primaryKey);
        Logger.HydratingState(primaryKey, currentPosition.Value);

        // Hydrate state from brook
        await foreach (BrookEvent brookEvent in BrookGrainFactory.GetBrookReaderGrain(brookKey)
                           .ReadEventsAsync(cancellationToken: token)
                           .WithCancellation(token))
        {
            object eventData = DeserializeEvent(brookEvent);
            state = RootReducer.Reduce(state!, eventData);
            currentPosition = new(currentPosition.Value + 1);
        }
    }

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

        // Check optimistic concurrency
        if (expectedVersion.HasValue && (expectedVersion.Value != currentPosition))
        {
            string message =
                $"Expected version {expectedVersion.Value.Value} but current version is {currentPosition.Value}.";
            Logger.CommandFailed(commandTypeName, aggregateKey, AggregateErrorCodes.ConcurrencyConflict, message);
            return OperationResult.Fail(AggregateErrorCodes.ConcurrencyConflict, message);
        }

        // Resolve command handler
        ICommandHandler<TCommand, TState>? handler = ServiceProvider.GetService<ICommandHandler<TCommand, TState>>();
        if (handler is null)
        {
            string message = $"No command handler registered for command type {commandTypeName}.";
            Logger.CommandFailed(commandTypeName, aggregateKey, AggregateErrorCodes.CommandHandlerNotFound, message);
            return OperationResult.Fail(AggregateErrorCodes.CommandHandlerNotFound, message);
        }

        // Execute handler
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        if (!result.Success)
        {
            Logger.CommandFailed(commandTypeName, aggregateKey, result.ErrorCode, result.ErrorMessage);
            return result.ToResult();
        }

        // Persist events if any were produced
        IReadOnlyList<object> events = result.Value;
        if (events.Count > 0)
        {
            ImmutableArray<BrookEvent> brookEvents = ConvertToBrookEvents(events);

            // Pass null for first write (when position is NotSet/-1), otherwise pass current position
            BrookPosition? expectedCursorPosition = currentPosition.NotSet ? null : currentPosition;
            BrookPosition newPosition = await BrookGrainFactory.GetBrookWriterGrain(brookKey)
                .AppendEventsAsync(brookEvents, expectedCursorPosition, cancellationToken);

            // Apply events to local state
            foreach (object eventData in events)
            {
                state = RootReducer.Reduce(state!, eventData);
            }

            currentPosition = newPosition;
        }

        Logger.CommandExecuted(commandTypeName, aggregateKey);
        return OperationResult.Ok();
    }

    /// <summary>
    ///     Resolves an event type from its string name.
    /// </summary>
    /// <param name="eventTypeName">The event type name.</param>
    /// <returns>The event type, or null if not found.</returns>
    /// <remarks>
    ///     Override this method to provide custom event type resolution logic.
    ///     The default implementation uses the event type registry from the service provider.
    /// </remarks>
    protected virtual Type? ResolveEventType(
        string eventTypeName
    )
    {
        IEventTypeRegistry? registry = ServiceProvider.GetService<IEventTypeRegistry>();
        return registry?.ResolveType(eventTypeName);
    }

    private ImmutableArray<BrookEvent> ConvertToBrookEvents(
        IReadOnlyList<object> events
    )
    {
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(events.Count);
        foreach (object eventData in events)
        {
            Type eventType = eventData.GetType();
            string eventTypeName = EventNameHelper.GetEventName(eventType);
            ReadOnlyMemory<byte> data = SerializationProvider.Write(eventData);
            builder.Add(
                new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    EventType = eventTypeName,
                    Source = brookKey,
                    Data = data.ToArray().ToImmutableArray(),
                    DataContentType = SerializationProvider.Format,
                });
        }

        return builder.ToImmutable();
    }

    private object DeserializeEvent(
        BrookEvent brookEvent
    )
    {
        // For now, we need to resolve the event type from the registry
        // This is a simplified implementation - in production, you'd have an event type registry
        Type? eventType = ResolveEventType(brookEvent.EventType);
        if (eventType is null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve event type '{brookEvent.EventType}'. " +
                "Ensure the event type is registered in the event type registry.");
        }

        return SerializationProvider.Read<object>(brookEvent.Data.AsMemory());
    }
}