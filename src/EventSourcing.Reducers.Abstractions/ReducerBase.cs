namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Abstract base class for reducers that handle a specific event type.
/// </summary>
/// <typeparam name="TModel">The type of the model being reduced.</typeparam>
/// <typeparam name="TEvent">The type of the event this reducer handles.</typeparam>
/// <remarks>
///     This base class provides type safety and filtering logic for reducers.
///     Derived classes only need to implement the Apply method for their specific event type.
/// </remarks>
public abstract class ReducerBase<TModel, TEvent> : IReducer<TModel, object>
{
    /// <summary>
    ///     Applies the event to the model if it is of the correct type.
    /// </summary>
    /// <param name="model">The current model state.</param>
    /// <param name="event">The event to apply.</param>
    /// <returns>
    ///     A new model instance with the event applied if the event type matches,
    ///     otherwise the original model unchanged.
    /// </returns>
    public TModel Reduce(
        TModel model,
        object @event
    )
    {
        if (@event is TEvent typedEvent)
        {
            return Apply(model, typedEvent);
        }

        return model;
    }

    /// <summary>
    ///     Applies the strongly-typed event to the model.
    /// </summary>
    /// <param name="model">The current model state.</param>
    /// <param name="event">The strongly-typed event to apply.</param>
    /// <returns>A new model instance with the event applied.</returns>
    /// <remarks>
    ///     Implementations must ensure that the original model is not mutated.
    ///     The returned model should be a new instance reflecting the changes from the event.
    /// </remarks>
    protected abstract TModel Apply(
        TModel model,
        TEvent @event
    );
}
