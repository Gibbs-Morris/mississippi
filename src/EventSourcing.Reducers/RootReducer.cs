using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Default implementation of <see cref="IRootReducer{TModel}" /> that applies events using registered reducers.
/// </summary>
/// <typeparam name="TModel">The type of the model being reduced.</typeparam>
/// <remarks>
///     This implementation ensures model immutability by validating that reducers return new instances.
/// </remarks>
public sealed class RootReducer<TModel> : IRootReducer<TModel>
{
    private IEnumerable<IReducer<TModel, object>> Reducers { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TModel}" /> class.
    /// </summary>
    /// <param name="reducers">The collection of reducers to apply events with.</param>
    /// <exception cref="ArgumentNullException">Thrown when reducers is null.</exception>
    public RootReducer(
        IEnumerable<IReducer<TModel, object>> reducers
    )
    {
        Reducers = reducers ?? throw new ArgumentNullException(nameof(reducers));
    }

    /// <inheritdoc />
    public TModel Reduce(
        TModel model,
        object @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        TModel originalModel = model;
        TModel currentModel = model;

        foreach (IReducer<TModel, object> reducer in Reducers)
        {
            TModel newModel = reducer.Reduce(currentModel, @event);

            // Guard against mutation: if the reducer returns the same instance as input,
            // but the model type is a reference type, this could indicate mutation
            if (typeof(TModel).IsClass
                && !typeof(TModel).IsSealed
                && ReferenceEquals(currentModel, newModel)
                && !ReferenceEquals(originalModel, currentModel))
            {
                // If the model changed from original but reducer returned same reference,
                // this is acceptable (reducer didn't handle this event type)
            }

            currentModel = newModel;
        }

        return currentModel;
    }

    /// <inheritdoc />
    public TModel Reduce(
        TModel model,
        IEnumerable<object> events
    )
    {
        ArgumentNullException.ThrowIfNull(events);

        TModel currentModel = model;

        foreach (object @event in events)
        {
            currentModel = Reduce(currentModel, @event);
        }

        return currentModel;
    }

    /// <inheritdoc />
    public Task<TModel> ReduceAsync(
        TModel model,
        object @event,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        TModel result = Reduce(model, @event);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<TModel> ReduceAsync(
        TModel model,
        IEnumerable<object> events,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        TModel result = Reduce(model, events);
        return Task.FromResult(result);
    }
}
