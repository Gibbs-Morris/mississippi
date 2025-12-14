namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Defines the contract for a reducer that applies a specific event type to a model.
/// </summary>
/// <typeparam name="TModel">The type of the model being reduced.</typeparam>
/// <typeparam name="TEvent">The type of the event being applied.</typeparam>
public interface IReducer<TModel, in TEvent>
{
    /// <summary>
    ///     Applies an event to the model, producing a new immutable model instance.
    /// </summary>
    /// <param name="model">The current model state before applying the event.</param>
    /// <param name="event">The event to apply to the model.</param>
    /// <returns>A new model instance with the event applied.</returns>
    /// <remarks>
    ///     Implementations must ensure that the original model is not mutated.
    ///     The returned model should be a new instance reflecting the changes from the event.
    /// </remarks>
    TModel Reduce(
        TModel model,
        TEvent @event
    );
}
