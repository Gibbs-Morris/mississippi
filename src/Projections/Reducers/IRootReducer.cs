namespace Mississippi.Projections.Reducers;

/// <summary>
///     Aggregates a sequence of reducers for a projection model and applies them to incoming events.
/// </summary>
/// <typeparam name="TModel">The projection model type that the reducers operate on.</typeparam>
public interface IRootReducer<TModel>
    where TModel : notnull, new()
{
    /// <summary>
    ///     Applies the registered reducers to the supplied model using the provided domain event.
    /// </summary>
    /// <param name="model">The current model state.</param>
    /// <param name="domainEvent">The domain event that should update the model.</param>
    /// <returns>The updated model after all reducers have executed.</returns>
    TModel Reduce(
        TModel model,
        object domainEvent
    );
}