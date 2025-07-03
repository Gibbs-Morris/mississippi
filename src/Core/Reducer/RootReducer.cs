using System.Collections.Concurrent;
using System.Collections.Immutable;


namespace Mississippi.Core.Reducer;

/// <summary>
///     High-performance implementation that caches a dispatch table
///     (<c>eventType → reducers[]</c>) for <c>O(1)</c> handler resolution.
/// </summary>
/// <typeparam name="TState">Aggregate-root state type.</typeparam>
internal sealed class RootReducer<TState> : IRootReducer<TState>
{
    /// <summary>
    ///     All reducers, preserved in DI registration order to guarantee deterministic results.
    /// </summary>
    private readonly ImmutableArray<IReducer<TState>> allReducers;

    /// <summary>
    ///     Maps each encountered event type to the ordered reducer list able to handle it.
    /// </summary>
    private readonly ConcurrentDictionary<Type, ImmutableArray<IReducer<TState>>> dispatchTable = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TState}" /> class.
    /// </summary>
    /// <param name="reducers">All registered reducers for <typeparamref name="TState" />.</param>
    public RootReducer(
        IEnumerable<IReducer<TState>> reducers
    )
    {
        allReducers = (reducers ?? Enumerable.Empty<IReducer<TState>>()).ToImmutableArray();

        // Seed the table for reducers whose event-type is known a-priori.
        foreach (IEventAwareReducer aware in allReducers.OfType<IEventAwareReducer>())
        {
            IReducer<TState> reducer = (IReducer<TState>)aware;
            dispatchTable.AddOrUpdate(
                aware.SupportedEventType,
                _ => ImmutableArray.Create(reducer),
                (
                    _,
                    list
                ) => list.Add(reducer));
        }
    }

    /// <inheritdoc />
    public TState Reduce<TEvent>(
        TState state,
        TEvent eventToReduce
    )
    {
        Type eventType = typeof(TEvent);

        // Resolve — or build once — the handler list for this event type.
        ImmutableArray<IReducer<TState>> handlers = dispatchTable.GetOrAdd(
            eventType,
            type => allReducers.Where(r => (r as IEventAwareReducer)?.CanHandle(type) ?? true).ToImmutableArray());
        foreach (IReducer<TState> handler in handlers)
        {
            state = handler.Reduce(state, eventToReduce);
        }

        return state;
    }
}