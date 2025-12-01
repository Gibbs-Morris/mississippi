namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Represents a component that updates a model in response to a single event.
/// </summary>
/// <typeparam name="TModel">The type of model being updated.</typeparam>
public interface IReducer<TModel>
    where TModel : class
{
    /// <summary>
    ///     Applies the supplied event to the provided model instance.
    /// </summary>
    /// <param name="input">The immutable model snapshot prior to handling the event.</param>
    /// <param name="eventData">The event being handled.</param>
    /// <returns>The updated model.</returns>
    /// <remarks>
    ///     Implementations MUST treat <paramref name="input" /> as immutable. If a reducer needs to change
    ///     state it MUST return a new <typeparamref name="TModel" /> instance that represents the updated
    ///     model rather than mutating the provided reference. Immutable enforcement relies on value equality,
    ///     so mutable reference types SHOULD override equality and hash-code semantics to enable guard rails.
    /// </remarks>
    TModel Reduce(
        TModel input,
        object eventData
    );
}