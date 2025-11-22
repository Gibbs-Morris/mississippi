namespace Mississippi.Projections.Reducers;

/// <summary>
///     Base class that simplifies reducer implementations by handling event type checks.
/// </summary>
/// <typeparam name="TModel">The projection model type.</typeparam>
/// <typeparam name="TEvent">The event type the reducer responds to.</typeparam>
public abstract class ReducerBase<TModel, TEvent> : IReducer<TModel>
    where TModel : notnull, new()
{
    /// <inheritdoc />
    public TModel Reduce(
        TModel model,
        object domainEvent
    ) =>
        domainEvent is not TEvent typedEvent ? model : Apply(model, typedEvent);

    /// <summary>
    ///     Applies the reducer logic when the supplied event matches the configured <typeparamref name="TEvent" /> type.
    /// </summary>
    /// <param name="model">The current projection model.</param>
    /// <param name="domainEvent">The strongly typed domain event.</param>
    /// <returns>The updated projection model.</returns>
    protected abstract TModel Apply(
        TModel model,
        TEvent domainEvent
    );
}