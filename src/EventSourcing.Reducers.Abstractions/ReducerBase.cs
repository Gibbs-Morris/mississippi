namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Provides a template for reducers that only handle a specific event type.
/// </summary>
/// <typeparam name="TModel">The model type operated on by the reducer.</typeparam>
/// <typeparam name="TEvent">The event type handled by the reducer.</typeparam>
public abstract class ReducerBase<TModel, TEvent> : IReducer<TModel>
    where TModel : class
{
    /// <summary>
    ///     Reduces the model in response to a strongly typed event.
    /// </summary>
    /// <param name="input">The model before the event is applied.</param>
    /// <param name="eventData">The strongly typed event.</param>
    /// <returns>The updated model.</returns>
    /// <remarks>
    ///     Implementations MUST treat <paramref name="input" /> as immutable and return a new model instance
    ///     whenever state changes are required.
    /// </remarks>
    public abstract TModel Reduce(
        TModel input,
        TEvent eventData
    );

    /// <inheritdoc />
    public TModel Reduce(
        TModel input,
        object eventData
    )
    {
        if (eventData is TEvent typedEvent)
        {
            return Reduce(input, typedEvent);
        }

        return input;
    }
}