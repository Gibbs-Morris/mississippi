namespace Mississippi.Projections.Reducers;

/// <summary>
///     Defines the contract for a single reducer that can mutate a projection model in response to an event.
/// </summary>
/// <typeparam name="TModel">The projection model type.</typeparam>
public interface IReducer<TModel>
    where TModel : notnull, new()
{
    /// <summary>
    ///     Applies the reducer logic to the provided model if the event is applicable.
    /// </summary>
    /// <param name="model">The current model state.</param>
    /// <param name="domainEvent">The domain event to evaluate.</param>
    /// <returns>The potentially updated model.</returns>
    TModel Reduce(
        TModel model,
        object domainEvent
    );
}