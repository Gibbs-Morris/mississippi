using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Adapter that allows a reducer to be expressed as a delegate.
/// </summary>
/// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
/// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
public sealed class DelegateReducer<TEvent, TProjection> : ReducerBase<TEvent, TProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DelegateReducer{TEvent, TProjection}" /> class.
    /// </summary>
    /// <param name="reduce">The delegate invoked to apply events to the current projection.</param>
    /// <param name="logger">The logger used for reducer diagnostics.</param>
    public DelegateReducer(
        Func<TProjection, TEvent, TProjection> reduce,
        ILogger<DelegateReducer<TEvent, TProjection>>? logger = null
    )
    {
        ReduceDelegate = reduce ?? throw new ArgumentNullException(nameof(reduce));
        Logger = logger ?? NullLogger<DelegateReducer<TEvent, TProjection>>.Instance;
    }

    private ILogger<DelegateReducer<TEvent, TProjection>> Logger { get; }

    private Func<TProjection, TEvent, TProjection> ReduceDelegate { get; }

    /// <inheritdoc />
    protected override TProjection ReduceCore(
        TProjection state,
        TEvent eventData
    )
    {
        string projectionType = typeof(TProjection).Name;
        string eventType = typeof(TEvent).Name;
        Logger.DelegateReducerReducing(projectionType, eventType);
        TProjection projection = ReduceDelegate(state, eventData);
        if (!typeof(TProjection).IsValueType && state is not null && ReferenceEquals(state, projection))
        {
            Logger.DelegateReducerProjectionInstanceReused(projectionType, eventType);
            throw new InvalidOperationException(
                "Reducers must return a new projection instance. Use a copy/with expression instead of mutating state.");
        }

        Logger.DelegateReducerReduced(projectionType, eventType);
        return projection;
    }
}